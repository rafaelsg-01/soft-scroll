#!/usr/bin/env node
/**
 * Auto Version Bump Script for SoftScroll
 */

import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, '..');

const BUMP_LEVEL = { major: 3, minor: 2, patch: 1 };

const TYPE_TO_SECTION = {
  feat: 'Added',
  fix: 'Fixed',
  perf: 'Performance',
  revert: 'Changed',
};

const RELEASE_TYPES = ['feat', 'fix', 'perf', 'revert'];

export function determineBump(commits) {
  let highest = null;
  for (const c of commits) {
    let level = null;
    if (c.breaking) {
      level = 'major';
    } else if (c.type === 'feat') {
      level = 'minor';
    } else if (RELEASE_TYPES.includes(c.type)) {
      level = 'patch';
    }
    if (level && (highest === null || BUMP_LEVEL[level] > BUMP_LEVEL[highest])) {
      highest = level;
    }
  }
  return highest;
}

export function computeNewVersion(currentVersion, bump) {
  const match = /^(\d+)\.(\d+)\.(\d+)(?:-.+)?$/.exec(currentVersion);
  if (!match) throw new Error(`Invalid version: ${currentVersion}`);

  let [, major, minor, patch] = match;
  major = Number(major);
  minor = Number(minor);
  patch = Number(patch);

  if (bump === 'major') return `${major + 1}.0.0`;
  if (bump === 'minor') return `${major}.${minor + 1}.0`;
  if (bump === 'patch') return `${major}.${minor}.${patch + 1}`;
  throw new Error(`Unknown bump: ${bump}`);
}

export function parseReleaseAsFooter(body) {
  const match = /^release-as:\s*(\S+)\s*$/im.exec(body);
  return match ? match[1] : null;
}

export function parseCommitMessage(raw) {
  const lines = raw.split('\n');
  const subjectLine = lines[0] || '';
  const body = lines.slice(2).join('\n');

  const match = /^(\w+)(\([^)]*\))?(!)?:\s*(.+)$/.exec(subjectLine);
  if (!match) return null;

  const [, type, , bang, subject] = match;
  const breaking = Boolean(bang) || /^BREAKING CHANGE:/m.test(body);

  return { type, subject, body, breaking };
}

export function readCurrentVersion() {
  const csprojPath = resolve(REPO_ROOT, 'SmoothScrollClone.csproj');
  const csproj = readFileSync(csprojPath, 'utf8');
  const match = /<Version>([^<]+)<\/Version>/.exec(csproj);
  if (!match) throw new Error('Cannot find version in SmoothScrollClone.csproj');
  return match[1].trim();
}

function updateAllVersionFiles(newVersion) {
  // 1. Update csproj
  const csprojPath = resolve(REPO_ROOT, 'SmoothScrollClone.csproj');
  if (existsSync(csprojPath)) {
    let content = readFileSync(csprojPath, 'utf8');
    content = content.replace(/<Version>[^<]+<\/Version>/g, `<Version>${newVersion}</Version>`)
                     .replace(/<AssemblyVersion>[^<]+<\/AssemblyVersion>/g, `<AssemblyVersion>${newVersion}</AssemblyVersion>`)
                     .replace(/<FileVersion>[^<]+<\/FileVersion>/g, `<FileVersion>${newVersion}</FileVersion>`)
                     .replace(/<InformationalVersion>[^<]+<\/InformationalVersion>/g, `<InformationalVersion>${newVersion}</InformationalVersion>`);
    writeFileSync(csprojPath, content);
    console.log(`Updated SmoothScrollClone.csproj to ${newVersion}`);
  }

  // 2. Update Inno Setup script
  const issPath = resolve(REPO_ROOT, 'installer/SoftScroll.iss');
  if (existsSync(issPath)) {
    let content = readFileSync(issPath, 'utf8');
    content = content.replace(/#define MyAppVersion "[^"]+"/, `#define MyAppVersion "${newVersion}"`);
    writeFileSync(issPath, content);
    console.log(`Updated installer/SoftScroll.iss to ${newVersion}`);
  }
}

function getCommits(lastTag) {
  const SEP = '<<<COMMIT_END>>>';
  const FIELD_SEP = '<<<FIELD>>>';
  const format = `%H${FIELD_SEP}%B${SEP}`;
  const args = ['log', `--pretty=format:${format}`];
  if (lastTag) args.splice(1, 0, `${lastTag}..HEAD`);

  let raw;
  try {
    raw = execFileSync('git', args, { cwd: REPO_ROOT, encoding: 'utf8' });
  } catch {
    raw = execFileSync('git', ['log', `--pretty=format:${format}`], {
      cwd: REPO_ROOT,
      encoding: 'utf8',
    });
  }

  const entries = raw.split(SEP).map((s) => s.trim()).filter(Boolean);
  const commits = [];
  for (const entry of entries) {
    const [hash, ...rest] = entry.split(FIELD_SEP);
    const fullMsg = rest.join(FIELD_SEP);
    const parsed = parseCommitMessage(fullMsg);
    if (parsed) commits.push({ ...parsed, hash });
  }
  return commits;
}

function setGithubOutput(key, value) {
  const out = process.env.GITHUB_OUTPUT;
  if (!out) return;
  writeFileSync(out, `${key}=${value}\n`, { flag: 'a' });
}

async function main() {
  const lastTag = process.env.LAST_TAG || '';
  const headMsg = process.env.HEAD_COMMIT_MSG || '';

  if (/\[skip release\]/i.test(headMsg)) {
    console.log('Detected [skip release]. Skipping.');
    process.exit(78);
  }

  const commits = getCommits(lastTag);
  console.log(`Analyzing ${commits.length} commit(s)`);

  const releaseAs = parseReleaseAsFooter(headMsg);
  const currentVersion = readCurrentVersion();
  let newVersion;

  if (releaseAs) {
    console.log(`Release-As: ${releaseAs}`);
    newVersion = releaseAs;
  } else {
    const bump = determineBump(commits);
    if (!bump) {
      console.log('No commits trigger release. Skipping.');
      process.exit(78);
    }
    newVersion = computeNewVersion(currentVersion, bump);
    console.log(`Bump ${bump}: ${currentVersion} -> ${newVersion}`);
  }

  updateAllVersionFiles(newVersion);

  const isPrerelease = /-/.test(newVersion);
  setGithubOutput('version', newVersion);
  setGithubOutput('tag', `v${newVersion}`);
  setGithubOutput('is_prerelease', String(isPrerelease));
  console.log(`Done: version=${newVersion}, tag=v${newVersion}`);
}

const invokedFromCli = process.argv[1] && resolve(process.argv[1]) === resolve(__filename);
if (invokedFromCli) {
  main().catch((err) => {
    console.error(err);
    process.exit(1);
  });
}
