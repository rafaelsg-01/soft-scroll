namespace SoftScroll;

public static class ScrollConstants
{
    // Mouse wheel
    public const int WHEEL_DELTA = 120;
    
    // Emission
    public const int EMIT_UNIT = 12;
    
    // Step calculation
    public const double BASE_STEP_PX = 120.0;
    
    // Pulse clamping
    public const int PULSE_CLAMP_MIN = -20;
    public const int PULSE_CLAMP_MAX = 20;
    
    // Frame rate
    public const int FRAME_RATE = 120;
    public const double FRAME_MS = 1000.0 / FRAME_RATE;
    
    // Spin wait for idle CPU
    public const int SPIN_WAIT_COUNT = 10;
}