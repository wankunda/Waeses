using System.ComponentModel;
using System.Reflection;

namespace WaesesUI;

public enum UiColor
{
    Primary,
    Secondary,
    Success,
    Danger,
    Warning,
    Info,
    Neutral
}

public enum UiSize
{
    Xs,
    Sm,
    Md,
    Lg,
    Xl,
    Max
}

public enum UiDensity
{
    Compact,
    Comfortable,
    Spacious
}

public enum UiTheme
{
    System,
    Light,
    Dark
}

public enum UiSkeletonVariant
{
    Text,
    Rect,
    Circle
}

public enum UiDrawerPosition
{
    Left,
    Right
}

public enum UiMenuAlignment
{
    Start,
    End
}

public enum UiDateTimeMode
{
    Date,
    Time,
    DateTime
}

public enum AvatarStatus
{
    None,
    Online,
    Busy,
    Away,
    Offline
}

public enum AvatarShape
{
    Circle,
    Square
}

public enum ImageRatio
{
    Auto,
    Square,     // 1:1
    Widescreen, // 16:9
    Standard,   // 4:3
    Photo       // 3:2
}

public enum UiButtonVariant
{
    Primary,
    Secondary,
    Success,
    Danger,
    Warning,
    Info,
    Outline,
    Ghost,
    Link
}

public enum ToastType
{
    Info,
    Success,
    Warning,
    Danger
}

public enum ToastPosition
{
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft,
    TopCenter,
    BottomCenter
}

public enum TooltipPosition
{
    Top,
    Bottom,
    Left,
    Right
}

public enum FileUploadVariant
{
    Dropzone,
    Button
}

public enum MessageBoxType
{
    Info,
    Success,
    Warning,
    Danger,
    Confirm
}

public enum UiSpinnerVariant
{
    Spin,
    Pulse
}

public enum Alignement
{
    Left,
    Center,
    Right
}

public static class EnumExtensions
{
    public static string ToDescription(this Enum value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString())!;

        DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes != null && attributes.Length > 0)
        {
            return attributes[0].Description;
        }
        else
        {
            return value.ToString();
        }
    }
}
/// <summary>Animation de passage d'une diapositive à l'autre dans <see cref="UiCarousel"/>.</summary>
public enum UiCarouselTransition
{
    Slide,
    Fade,
    None
}
