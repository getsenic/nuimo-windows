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
        //TouchTop seems not to work
        TouchTop,
        TouchBottom,
        LongTouchLeft,
        LongTouchRight,
        //LongTouchTop seems not to work
        LongTouchTop,
        LongTouchBottom,
        FlyLeft,
        FlyRight,
        //Removed from firmware?
        FlyBackwards,
        //Removed from firmware?
        FlyTowards,
        FlyUpDown
    }

    public enum NuimoColor
    {
        Unknown,
        Black,
        //TODO White or Silver?
        Silver
    }
}