import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  determineBump,
  computeNewVersion,
  parseReleaseAsFooter,
  parseCommitMessage,
  readCurrentVersion,
} from './version-bump.mjs';

test('determineBump: returns "major" for breaking changes', () => {
  const commits = [{ type: 'feat', breaking: true }];
  assert.equal(determineBump(commits), 'major');
});

test('determineBump: returns "minor" for feat', () => {
  const commits = [{ type: 'feat', breaking: false }];
  assert.equal(determineBump(commits), 'minor');
});

test('determineBump: returns "patch" for fix', () => {
  const commits = [{ type: 'fix', breaking: false }];
  assert.equal(determineBump(commits), 'patch');
});

test('determineBump: returns null for non-release types', () => {
  const commits = [
    { type: 'docs', breaking: false },
    { type: 'chore', breaking: false },
  ];
  assert.equal(determineBump(commits), null);
});

test('computeNewVersion: bumps patch', () => {
  assert.equal(computeNewVersion('1.0.0', 'patch'), '1.0.1');
});

test('computeNewVersion: bumps minor', () => {
  assert.equal(computeNewVersion('1.0.0', 'minor'), '1.1.0');
});

test('computeNewVersion: bumps major', () => {
  assert.equal(computeNewVersion('1.0.0', 'major'), '2.0.0');
});

test('parseReleaseAsFooter: extracts version', () => {
  const body = 'Message\n\nRelease-As: 2.0.0';
  assert.equal(parseReleaseAsFooter(body), '2.0.0');
});

test('parseCommitMessage: parses feat', () => {
  const parsed = parseCommitMessage('feat: add feature');
  assert.equal(parsed.type, 'feat');
  assert.equal(parsed.breaking, false);
});

test('parseCommitMessage: parses merge commit body', () => {
  const raw = 'Merge pull request #19 from foo/fix-scroll\n\nfix: correct scroll direction';
  const parsed = parseCommitMessage(raw);
  assert.equal(parsed.type, 'fix');
  assert.equal(parsed.breaking, false);
});

test('parseCommitMessage: returns null when no conventional commit found', () => {
  assert.equal(parseCommitMessage('Merge pull request #5 from user/branch\n\nNo conventional prefix here'), null);
});

test('parseCommitMessage: detects bang', () => {
  const parsed = parseCommitMessage('feat!: breaking');
  assert.equal(parsed.breaking, true);
});

test('readCurrentVersion: reads correctly', () => {
  assert.ok(readCurrentVersion());
});
