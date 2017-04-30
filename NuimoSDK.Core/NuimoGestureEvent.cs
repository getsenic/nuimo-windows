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
        TouchLeft,
        TouchRight,
        
        /// <remarks>
        /// Reserved for future use
        /// </remarks>
        TouchTop,
        TouchBottom,

        LongTouchLeft,
        LongTouchRight,

        /// <remarks>
        /// Reserved for future use
        /// </remarks>
        LongTouchTop,
        LongTouchBottom,

        FlyLeft,
        FlyRight,
        
        FlyUpDown
    }
}