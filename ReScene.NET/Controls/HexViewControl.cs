using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;

namespace ReScene.NET.Controls;

public class HexViewControl : UserControl
{
    private const int BytesPerLine = 16;
    private const double CharWidth = 8.4;
    private const double LineHeight = 18;

    private const double AddressWidth = 10 * CharWidth;
    private const double HexWidth = 47 * CharWidth;
    private const double AsciiWidth = 16 * CharWidth;
    private const double GapWidth = 2 * CharWidth;

    public static readonly StyledProperty<byte[]?> DataProperty =
        AvaloniaProperty.Register<HexViewControl, byte[]?>(nameof(Data));

    public static readonly StyledProperty<long> BlockOffsetProperty =
        AvaloniaProperty.Register<HexViewControl, long>(nameof(BlockOffset));

    public static readonly StyledProperty<int> BlockLengthProperty =
        AvaloniaProperty.Register<HexViewControl, int>(nameof(BlockLength));

    public static readonly StyledProperty<long> SelectionOffsetProperty =
        AvaloniaProperty.Register<HexViewControl, long>(nameof(SelectionOffset), defaultValue: -1);

    public static readonly StyledProperty<int> SelectionLengthProperty =
        AvaloniaProperty.Register<HexViewControl, int>(nameof(SelectionLength));

    private readonly HexCanvas _canvas;
    private readonly ScrollViewer _scrollViewer;

    public byte[]? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public long BlockOffset
    {
        get => GetValue(BlockOffsetProperty);
        set => SetValue(BlockOffsetProperty, value);
    }

    public int BlockLength
    {
        get => GetValue(BlockLengthProperty);
        set => SetValue(BlockLengthProperty, value);
    }

    public long SelectionOffset
    {
        get => GetValue(SelectionOffsetProperty);
        set => SetValue(SelectionOffsetProperty, value);
    }

    public int SelectionLength
    {
        get => GetValue(SelectionLengthProperty);
        set => SetValue(SelectionLengthProperty, value);
    }

    static HexViewControl()
    {
        DataProperty.Changed.AddClassHandler<HexViewControl>((c, _) => c.RefreshCanvas());
        BlockOffsetProperty.Changed.AddClassHandler<HexViewControl>((c, _) => c.RefreshCanvas());
        BlockLengthProperty.Changed.AddClassHandler<HexViewControl>((c, _) => c.RefreshCanvas());
        SelectionOffsetProperty.Changed.AddClassHandler<HexViewControl>((c, _) =>
        {
            c._canvas.ClearMouseSelection();
            c._canvas.InvalidateVisual();
        });
        SelectionLengthProperty.Changed.AddClassHandler<HexViewControl>((c, _) => c.OnSelectionChanged());
    }

    public HexViewControl()
    {
        _canvas = new HexCanvas(this)
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _canvas
        };

        // Repaint when the user scrolls so we render the newly-visible lines
        _scrollViewer.ScrollChanged += (_, _) => _canvas.InvalidateVisual();

        Content = _scrollViewer;
    }

    private void RefreshCanvas()
    {
        int lineCount = BlockLength > 0 ? (BlockLength + BytesPerLine - 1) / BytesPerLine : 0;
        _canvas.Height = Math.Max(lineCount * LineHeight, 1);
        _canvas.Width = AddressWidth + GapWidth + HexWidth + GapWidth + AsciiWidth + 20;
        _canvas.InvalidateVisual();
    }

    private void OnSelectionChanged()
    {
        _canvas.InvalidateVisual();

        // Scroll to selection
        if (SelectionOffset >= 0 && BlockLength > 0)
        {
            long relOffset = SelectionOffset - BlockOffset;
            if (relOffset >= 0 && relOffset < BlockLength)
            {
                int lineIndex = (int)(relOffset / BytesPerLine);
                double targetY = lineIndex * LineHeight;
                double viewportH = _scrollViewer.Viewport.Height;
                double currentY = _scrollViewer.Offset.Y;

                if (targetY < currentY || targetY > currentY + viewportH - LineHeight)
                {
                    _scrollViewer.Offset = new Vector(0, Math.Max(0, targetY - viewportH / 3));
                }
            }
        }
    }

    private class HexCanvas : Control, ICustomHitTest
    {
        private readonly HexViewControl _owner;

        private static readonly Typeface MonoTypeface = new("Cascadia Mono, Consolas, Courier New, monospace");
        private static readonly IBrush SelectionBrush = new SolidColorBrush(Color.FromArgb(80, 60, 120, 220));

        // Mouse selection state
        private long _mouseSelAnchor = -1;
        private long _mouseSelCurrent = -1;
        private bool _isMouseSelecting;
        private bool _isAsciiAreaSelection;

        private long MouseSelStart => _mouseSelAnchor < 0 ? -1 : Math.Min(_mouseSelAnchor, _mouseSelCurrent);
        private long MouseSelEnd => _mouseSelAnchor < 0 ? -1 : Math.Max(_mouseSelAnchor, _mouseSelCurrent);
        private long MouseSelLength => _mouseSelAnchor < 0 ? 0 : MouseSelEnd - MouseSelStart + 1;

        public HexCanvas(HexViewControl owner)
        {
            _owner = owner;
            Focusable = true;
            Cursor = new Cursor(StandardCursorType.Ibeam);

            var copyHex = new MenuItem { Header = "Copy as Hex" };
            copyHex.Click += (_, _) => CopyToClipboard(asText: false);

            var copyText = new MenuItem { Header = "Copy as Text" };
            copyText.Click += (_, _) => CopyToClipboard(asText: true);

            var selectAll = new MenuItem { Header = "Select All" };
            selectAll.Click += (_, _) => SelectAll();

            var menu = new ContextMenu { Items = { copyHex, copyText, new Separator(), selectAll } };
            menu.Opening += (_, _) =>
            {
                GetActiveSelection(out long s, out long l);
                bool hasSel = s >= 0 && l > 0;
                copyHex.IsEnabled = hasSel;
                copyText.IsEnabled = hasSel;
            };
            ContextMenu = menu;
        }

        // Ensure the entire canvas area is hit-testable for pointer events
        public bool HitTest(Point point) => true;

        public void ClearMouseSelection()
        {
            _mouseSelAnchor = -1;
            _mouseSelCurrent = -1;
            _isMouseSelecting = false;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                Focus();
                var pos = e.GetPosition(this);
                long byteOffset = HitTestByte(pos, out bool isAscii);

                if (byteOffset >= 0)
                {
                    _mouseSelAnchor = byteOffset;
                    _mouseSelCurrent = byteOffset;
                    _isMouseSelecting = true;
                    _isAsciiAreaSelection = isAscii;
                    e.Pointer.Capture(this);
                    InvalidateVisual();
                }

                e.Handled = true;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (_isMouseSelecting)
            {
                var pos = e.GetPosition(this);
                long byteOffset = HitTestByte(pos, out _);

                if (byteOffset >= 0 && byteOffset != _mouseSelCurrent)
                {
                    _mouseSelCurrent = byteOffset;
                    InvalidateVisual();
                }

                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_isMouseSelecting)
            {
                _isMouseSelecting = false;
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyModifiers == KeyModifiers.Control)
            {
                if (e.Key == Key.C)
                {
                    CopyToClipboard(asText: _isAsciiAreaSelection);
                    e.Handled = true;
                }
                else if (e.Key == Key.A)
                {
                    SelectAll();
                    e.Handled = true;
                }
            }
        }

        private void SelectAll()
        {
            if (_owner.BlockLength > 0)
            {
                _mouseSelAnchor = _owner.BlockOffset;
                _mouseSelCurrent = _owner.BlockOffset + _owner.BlockLength - 1;
                _isAsciiAreaSelection = false;
                InvalidateVisual();
            }
        }

        private void GetActiveSelection(out long selStart, out long selLength)
        {
            selStart = MouseSelStart;
            selLength = MouseSelLength;

            // Fall back to property-driven selection if no mouse selection
            if (selStart < 0 || selLength <= 0)
            {
                selStart = _owner.SelectionOffset;
                selLength = _owner.SelectionLength;
            }
        }

        private async void CopyToClipboard(bool asText)
        {
            GetActiveSelection(out long selStart, out long selLength);

            if (selStart < 0 || selLength <= 0 || _owner.Data == null)
                return;

            byte[] data = _owner.Data;
            int start = (int)Math.Max(0, selStart);
            int len = (int)Math.Min(selLength, data.Length - start);
            if (len <= 0) return;

            string text;
            if (asText)
            {
                var sb = new StringBuilder(len);
                for (int i = 0; i < len; i++)
                {
                    byte b = data[start + i];
                    sb.Append(b >= 0x20 && b <= 0x7E ? (char)b : '.');
                }
                text = sb.ToString();
            }
            else
            {
                var sb = new StringBuilder(len * 3);
                for (int i = 0; i < len; i++)
                {
                    if (i > 0) sb.Append(' ');
                    sb.Append(data[start + i].ToString("X2"));
                }
                text = sb.ToString();
            }

            var clipboard = TopLevel.GetTopLevel(_owner)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        }

        private long HitTestByte(Point pos, out bool isAsciiArea)
        {
            isAsciiArea = false;

            int blockLen = _owner.BlockLength;
            if (blockLen <= 0) return -1;

            int line = (int)(pos.Y / LineHeight);
            int totalLines = (blockLen + BytesPerLine - 1) / BytesPerLine;
            if (line < 0 || line >= totalLines) return -1;

            double hexStartX = AddressWidth + GapWidth;
            double hexEndX = hexStartX + HexWidth;
            double asciiStartX = hexEndX + GapWidth;
            double asciiEndX = asciiStartX + AsciiWidth;

            int byteInLine;

            if (pos.X >= hexStartX && pos.X < hexEndX)
            {
                byteInLine = (int)((pos.X - hexStartX) / (3 * CharWidth));
                byteInLine = Math.Clamp(byteInLine, 0, BytesPerLine - 1);
            }
            else if (pos.X >= asciiStartX && pos.X <= asciiEndX)
            {
                byteInLine = (int)((pos.X - asciiStartX) / CharWidth);
                byteInLine = Math.Clamp(byteInLine, 0, BytesPerLine - 1);
                isAsciiArea = true;
            }
            else
            {
                return -1;
            }

            int lineOffset = line * BytesPerLine + byteInLine;
            if (lineOffset >= blockLen) return -1;

            return _owner.BlockOffset + lineOffset;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_owner.Data == null || _owner.BlockLength <= 0)
                return;

            // Resolve theme-aware brushes at render time
            var addressBrush = GetBrush("SystemControlForegroundBaseMediumBrush", Brushes.Gray);
            var hexBrush = GetBrush("SystemControlForegroundBaseHighBrush", Brushes.Black);
            var asciiBrush = GetBrush("SystemControlForegroundBaseMediumHighBrush", Brushes.DimGray);

            byte[] data = _owner.Data;
            long blockStart = _owner.BlockOffset;
            int blockLen = _owner.BlockLength;

            // Mouse selection takes priority over property-driven selection
            long selStart;
            long selLen;
            if (_mouseSelAnchor >= 0)
            {
                selStart = MouseSelStart;
                selLen = MouseSelLength;
            }
            else
            {
                selStart = _owner.SelectionOffset;
                selLen = _owner.SelectionLength;
            }

            int totalLines = (blockLen + BytesPerLine - 1) / BytesPerLine;

            // Viewport-based virtualization: only render visible lines
            double scrollY = _owner._scrollViewer.Offset.Y;
            double viewportH = _owner._scrollViewer.Viewport.Height;
            int firstVisible = Math.Max(0, (int)(scrollY / LineHeight) - 1);
            int lastVisible = Math.Min(totalLines - 1, (int)((scrollY + viewportH) / LineHeight) + 1);

            for (int line = firstVisible; line <= lastVisible; line++)
            {
                double y = line * LineHeight;
                long lineFileOffset = blockStart + (long)line * BytesPerLine;
                int lineDataStart = line * BytesPerLine;
                int lineBytes = Math.Min(BytesPerLine, blockLen - lineDataStart);

                // Selection highlight
                if (selStart >= 0 && selLen > 0)
                {
                    long selEnd = selStart + selLen;
                    long lineEnd = lineFileOffset + BytesPerLine;

                    if (selStart < lineEnd && selEnd > lineFileOffset)
                    {
                        int highlightStart = (int)Math.Max(0, selStart - lineFileOffset);
                        int highlightEnd = (int)Math.Min(BytesPerLine, selEnd - lineFileOffset);

                        double hx1 = AddressWidth + GapWidth + highlightStart * 3 * CharWidth;
                        double hx2 = AddressWidth + GapWidth + (highlightEnd * 3 - 1) * CharWidth;
                        context.FillRectangle(SelectionBrush, new Rect(hx1, y, hx2 - hx1, LineHeight));

                        double ax1 = AddressWidth + GapWidth + HexWidth + GapWidth + highlightStart * CharWidth;
                        double ax2 = AddressWidth + GapWidth + HexWidth + GapWidth + highlightEnd * CharWidth;
                        context.FillRectangle(SelectionBrush, new Rect(ax1, y, ax2 - ax1, LineHeight));
                    }
                }

                // Address
                string addr = lineFileOffset.ToString("X8");
                var addrText = new FormattedText(addr, CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, MonoTypeface, 12, addressBrush);
                context.DrawText(addrText, new Point(0, y + 2));

                // Hex and ASCII
                if (lineFileOffset >= 0 && lineFileOffset < data.Length)
                {
                    int srcStart = (int)lineFileOffset;
                    int available = Math.Min(lineBytes, data.Length - srcStart);
                    if (available <= 0) continue;

                    var hexBuilder = new StringBuilder(BytesPerLine * 3);
                    var asciiBuilder = new StringBuilder(BytesPerLine);

                    for (int i = 0; i < available; i++)
                    {
                        byte b = data[srcStart + i];
                        if (i > 0) hexBuilder.Append(' ');
                        hexBuilder.Append(b.ToString("X2"));
                        asciiBuilder.Append(b >= 0x20 && b <= 0x7E ? (char)b : '.');
                    }

                    double hexX = AddressWidth + GapWidth;
                    var hexText = new FormattedText(hexBuilder.ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, MonoTypeface, 12, hexBrush);
                    context.DrawText(hexText, new Point(hexX, y + 2));

                    double asciiX = AddressWidth + GapWidth + HexWidth + GapWidth;
                    var asciiText = new FormattedText(asciiBuilder.ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, MonoTypeface, 12, asciiBrush);
                    context.DrawText(asciiText, new Point(asciiX, y + 2));
                }
            }
        }

        private IBrush GetBrush(string resourceKey, IBrush fallback)
        {
            if (_owner.TryFindResource(resourceKey, _owner.ActualThemeVariant, out var resource)
                && resource is IBrush brush)
                return brush;
            return fallback;
        }
    }
}
