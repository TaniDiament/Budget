namespace Budget.Models;

public sealed class WindowPlacement
{
    public double Left { get; set; } = double.NaN;

    public double Top { get; set; } = double.NaN;

    public double Width { get; set; } = 1180;

    public double Height { get; set; } = 800;

    public string WindowState { get; set; } = "Normal";
}

