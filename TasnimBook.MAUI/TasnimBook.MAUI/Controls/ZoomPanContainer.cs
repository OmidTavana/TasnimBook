namespace TasnimBook.MAUI.Controls;

/// <summary>A clipped viewport that supports focal-point pinch zoom and one-finger panning.</summary>
public sealed class ZoomPanContainer : ContentView
{
    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom),
        typeof(double),
        typeof(ZoomPanContainer),
        1d,
        defaultBindingMode: BindingMode.TwoWay,
        coerceValue: (_, value) => Math.Clamp((double)value, 0.75, 2.5),
        propertyChanged: OnZoomChanged);

    private double _pinchStartZoom = 1;
    private double _pinchStartX;
    private double _pinchStartY;
    private double _panStartX;
    private double _panStartY;

    public ZoomPanContainer()
    {
        IsClippedToBounds = true;

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        GestureRecognizers.Add(pinch);

        var pan = new PanGestureRecognizer { TouchPoints = 1 };
        pan.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(pan);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += (_, _) => Zoom = Math.Abs(Zoom - 1) < 0.05 ? 2 : 1;
        GestureRecognizers.Add(doubleTap);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        ApplyTransform();
    }

    private static void OnZoomChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var container = (ZoomPanContainer)bindable;
        if ((double)newValue <= 1)
        {
            container._pinchStartX = 0;
            container._pinchStartY = 0;
        }
        container.ApplyTransform();
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                _pinchStartZoom = Zoom;
                _pinchStartX = Content?.TranslationX ?? 0;
                _pinchStartY = Content?.TranslationY ?? 0;
                break;

            case GestureStatus.Running:
                var nextZoom = Math.Clamp(_pinchStartZoom * e.Scale, 0.75, 2.5);
                if (Content is not null)
                {
                    // Keep the point between the two fingers visually stationary.
                    var focalX = (e.ScaleOrigin.X - 0.5) * Width;
                    var focalY = (e.ScaleOrigin.Y - 0.5) * Height;
                    Content.TranslationX = _pinchStartX + (_pinchStartZoom - nextZoom) * focalX;
                    Content.TranslationY = _pinchStartY + (_pinchStartZoom - nextZoom) * focalY;
                }
                Zoom = nextZoom;
                ClampTranslation();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                ClampTranslation();
                break;
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (Content is null || Zoom <= 1) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartX = Content.TranslationX;
                _panStartY = Content.TranslationY;
                break;
            case GestureStatus.Running:
                Content.TranslationX = _panStartX + e.TotalX;
                Content.TranslationY = _panStartY + e.TotalY;
                ClampTranslation();
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                ClampTranslation();
                break;
        }
    }

    private void ApplyTransform()
    {
        if (Content is null) return;
        Content.Scale = Zoom;
        ClampTranslation();
    }

    private void ClampTranslation()
    {
        if (Content is null || Width <= 0 || Height <= 0) return;
        if (Zoom <= 1)
        {
            Content.TranslationX = 0;
            Content.TranslationY = 0;
            return;
        }

        var maxX = Math.Max(0, Width * (Zoom - 1) / 2);
        var maxY = Math.Max(0, Height * (Zoom - 1) / 2);
        Content.TranslationX = Math.Clamp(Content.TranslationX, -maxX, maxX);
        Content.TranslationY = Math.Clamp(Content.TranslationY, -maxY, maxY);
    }
}
