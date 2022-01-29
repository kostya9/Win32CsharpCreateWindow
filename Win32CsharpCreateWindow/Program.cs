using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

var window = CreateWindow();
RunMessageLoop(window);

static unsafe void RunMessageLoop(HWND window)
{
    MSG msg;
    // Get the message from the message queue
    while(GetMessageW(&msg, window, 0, 0) && !Closed)
    {
        // Dispatch the message to the WinProc function
        DispatchMessageW(&msg);
    }
}

[UnmanagedCallersOnly]
static unsafe LRESULT WinProc(HWND window, uint message, WPARAM wParam, LPARAM lParam)
{
    // Handling WM_PAINT message
    if (message == WM.WM_PAINT)
    {
        var ps = new PAINTSTRUCT();
        var deviceContextHandle = BeginPaint(window, &ps);

        // Filling in the background
        if (Redraw)
        {
            FillRect(deviceContextHandle, &ps.rcPaint, HBRUSH.NULL);
            Redraw = false;
        }

        // Hide the disappeared pixels by drawing them white
        // And removing them from the queu of last pixels
        while (LastPositions.Count > MaxPositions)
        {
            var extraPosition = LastPositions.Dequeue();

            SetPixel(deviceContextHandle, extraPosition.X, extraPosition.Y, RGB(255, 255, 255));
        }

        // Show the pixels by drawing them black
        foreach (var point in LastPositions)
        {
            SetPixel(deviceContextHandle, point.X, point.Y, RGB(0, 0, 0));
        }

        EndPaint(window, &ps);
        return 0; // We successfully handled the message
    }

    // Handling when mouse moved in the window
    if (message == WM.WM_MOUSEMOVE)
    {
        var xPos = (int)(short)LOWORD(lParam);   // horizontal position 
        var yPos = (int)(short)HIWORD(lParam);   // vertical position

        // Remember what was the last mouse position
        LastPositions.Enqueue(new MousePoint(xPos, yPos));

        // Force OS to draw the window (and send us WM_PAINT message)
        InvalidateRect(window, null, BOOL.FALSE);

        return 0;
    }

    // Handling when window was resized
    if (message == WM.WM_SIZE)
    {
        // When window was resized, we need to make sure that the background is there
        Redraw = true;
        return 0;
    }

    // Track when we need to stop the message loop
    if (message == WM.WM_CLOSE)
    {
        Closed = true;
        return 0;
    }

    // Ignoring everything else
    return DefWindowProcW(window, message, wParam, lParam);
}

static unsafe HWND CreateWindow()
{
    var className = "windowClass";

    fixed (char* classNamePtr = className) // Hey, GC, please don't move the string
    {
        var windowClass = new WNDCLASSEXW();
        windowClass.cbSize = (uint)sizeof(WNDCLASSEXW); // Size (in bytes) of WNDCLASSEXW structure
        windowClass.hbrBackground = HBRUSH.NULL;
        windowClass.hCursor = HCURSOR.NULL;
        windowClass.hIcon = HICON.NULL;
        windowClass.hIconSm = HICON.NULL;
        windowClass.hInstance = HINSTANCE.NULL;
        windowClass.lpszClassName = (ushort*)classNamePtr; // The UTF-16 window class name
        windowClass.lpszMenuName = null;
        windowClass.style = 0;
        windowClass.lpfnWndProc = &WinProc; // Pointer to WinProc function

        var classId = RegisterClassExW(&windowClass);
    }

    var windowName = "windowName";
    fixed (char* windowNamePtr = windowName) // Hey, GC, please don't move the string
    fixed (char* classNamePtr = className) // GC, do not move this one too
    {
        var width = 500;
        var height = 500;
        var x = 0;
        var y = 0;

        var styles = WS.WS_OVERLAPPEDWINDOW | WS.WS_VISIBLE; // The window style
        var exStyles = 0; // Extended styles that we do not care about

        return CreateWindowExW((uint)exStyles,
            (ushort*)classNamePtr,  // UTF-16 window class name
            (ushort*)windowNamePtr, // UTF-16 window name (this will be in the title bar)
            (uint)styles,
            x, y, // Window initial position
            width, height, // Window initial size
            HWND.NULL, HMENU.NULL, HINSTANCE.NULL, null);
    }
}

/// <summary>
/// Top-level statements in C# are wrapped in an implicit Program class.
/// We can extend that Program class via implementing the additional stuff in a partial class
/// </summary>
partial class Program
{
    private static bool Closed;

    private static bool Redraw;

    static Queue<MousePoint> LastPositions = new();

    const int MaxPositions = 200;

    record MousePoint(int X, int Y);
}