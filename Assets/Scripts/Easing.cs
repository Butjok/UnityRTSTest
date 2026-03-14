public static class Easing {

    public enum Type {
        Linear,
        InOutQuadratic
    }
    
    public static float Linear(float t) {
        return t;
    }
    public static float InOutQuadratic(float t) {
        return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
    }
    
    public static float Evaluate(Type type, float t) {
        return type switch {
            Type.Linear => Linear(t),
            Type.InOutQuadratic => InOutQuadratic(t),
            _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}