using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace GISSBotChallenge2016
{
    // Interop initialization
    // (used for CV)
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    class CVHelper
    {

        private MediaCapture _mediaCapture;
        private CaptureElement _captureElement;

        private bool _isPreviewing;

        public CVHelper()
        {

        }

        public async Task<DeviceInformationCollection> GetCamerasAsync()
        {
            return await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
        }

        public async Task InitializeAsync(DeviceInformation camera)
        {
            if (camera != null)
            {
                try
                {
                    _mediaCapture = new MediaCapture();
                    await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings { VideoDeviceId = camera.Id });
                }
                catch (Exception)
                {
                    _mediaCapture = null;
                }
            }
            else
            {
                _mediaCapture = null;
            }
        }

        public async Task StartPreviewAsync(CaptureElement captureElement)
        {
            _captureElement = captureElement;
            _captureElement.Source = null;
            _captureElement.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();
            _isPreviewing = true;
        }

        public async Task StopPreviewAsync()
        {
            await _mediaCapture.StopPreviewAsync();
            _captureElement.Source = null;
            _isPreviewing = false;
        }

        public void Dispose()
        {
            _mediaCapture.Dispose();
        }

        public async Task<SoftwareBitmap> GetFrameAsync()
        {
            if (_mediaCapture != null)
            {
                try
                {
                    LowLagPhotoCapture lowLagCapture = await _mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
                    CapturedPhoto capturedPhoto = await lowLagCapture.CaptureAsync();
                    SoftwareBitmap swBmp = capturedPhoto.Frame.SoftwareBitmap;
                    await lowLagCapture.FinishAsync();
                    return swBmp;
                }
                catch (COMException e)
                {
                    Debug.WriteLine(e);
                    return null;
                }
            }
            else
            {
                return new SoftwareBitmap(BitmapPixelFormat.Bgra8, 400, 300, BitmapAlphaMode.Ignore);
            }
        }

        public bool IsSimulator
        {
            get
            {
                return _mediaCapture == null;
            }
        }

        public bool IsPreviewing
        {
            get
            {
                return _isPreviewing;
            }
        }

        public MediaCapture Source
        {
            get
            {
                return _mediaCapture;
            }
        }

        // Graphics / Buffer helper functions

        public unsafe int[,] GetColorDistribution(SoftwareBitmap haystack, Color needle, int[] tolerance, int sparcity=2)
        {
            int w = haystack.PixelWidth;
            int h = haystack.PixelHeight;
            int r = needle.R;
            int g = needle.G;
            int b = needle.B;

            int[,] dA = new int[w * h, 2];
            int dAPointer = 0;

            BitmapBuffer buffer = haystack.LockBuffer(BitmapBufferAccessMode.Read);
            BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
            IMemoryBufferReference reference = buffer.CreateReference();
            ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* bufferData, out uint capacity);



            int pos;

            for (int x = 0; x < w; x+=sparcity)
            {
                for (int y = 0; y < h; y+=sparcity)
                {
                    pos = bufferLayout.StartIndex + bufferLayout.Stride * y + 4 * x;
                    int rC = bufferData[pos + 2];
                    int gC = bufferData[pos + 1];
                    int bC = bufferData[pos];
                    if (Math.Abs(rC - r) <= tolerance[0] && Math.Abs(gC - g) <= tolerance[1] && Math.Abs(bC - b) <= tolerance[2])
                    {
                        dA[dAPointer, 0] = x;
                        dA[dAPointer, 1] = y;
                        dAPointer++;
                    }
                }
            }
            dAPointer--;

            reference.Dispose();
            buffer.Dispose();

            int[,] distributionArray = new int[dAPointer+1,2];

            for (int p = dAPointer; p >= 0; p--)
            {
                distributionArray[p, 0] = dA[p, 0];
                distributionArray[p, 1] = dA[p, 1];
            }

            return distributionArray;
        }

        public unsafe Color GetPixelColor(SoftwareBitmap swBmp, int x, int y)
        {
            BitmapBuffer buffer = swBmp.LockBuffer(BitmapBufferAccessMode.Read);
            BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
            Color color = new Color();
            if (x >= 0 && x < bufferLayout.Width && y >= 0 && y < bufferLayout.Height)
            {
                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* bufferData, out uint capacity);

                    color.B = bufferData[bufferLayout.StartIndex + bufferLayout.Stride * y + 4 * x + 0];
                    color.G = bufferData[bufferLayout.StartIndex + bufferLayout.Stride * y + 4 * x + 1];
                    color.R = bufferData[bufferLayout.StartIndex + bufferLayout.Stride * y + 4 * x + 2];
                    color.A = bufferData[bufferLayout.StartIndex + bufferLayout.Stride * y + 4 * x + 3];
                }
            }
            buffer.Dispose();
            return color;
        }

        public unsafe void SetPixelColor(SoftwareBitmap swBmp, int x, int y, Color color)
        {
            BitmapBuffer buffer = swBmp.LockBuffer(BitmapBufferAccessMode.Write);
            BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
            if (x >= 0 && x < bufferLayout.Width && y >= 0 && y < bufferLayout.Height)
            {
                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* bufferData, out uint capacity);

                    bufferData[bufferLayout.StartIndex + bufferLayout.Stride * y + 4 * x + 0] = color.B;
                    bufferData[bufferLayout.StartIndex + bufferLayout.Stride * y + 4 * x + 1] = color.G;
                    bufferData[bufferLayout.StartIndex + bufferLayout.Stride * y + 4 * x + 2] = color.R;
                    bufferData[bufferLayout.StartIndex + bufferLayout.Stride * y + 4 * x + 3] = color.A;
                }
            }
            buffer.Dispose();
        }
    }
}
