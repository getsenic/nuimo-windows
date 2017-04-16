namespace NuimoSDK
{
    public class NuimoGestureEvent
    {
        public NuimoGestureEvent(NuimoGesture gesture, int value)
        {
            Gesture = gesture;
            Value = value;
        }

        public NuimoGesture Gesture { get; }
        public int Value { get; }
    }

    public enum NuimoGesture
    {
        ButtonPress,
        ButtonRelease,
        Rotate,
        SwipeLeft,
        SwipeRight,
        SwipeUp,
        SwipeDown,
        FlyLeft,
        FlyRight,
        FlyBackwards,
        FlyTowards,
        FlyUpDown
    }
}