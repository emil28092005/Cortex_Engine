namespace Engine.Graphics.Vulkan;

internal static class PngEncoder
{
    public static byte[] EncodeRgbaToPng(byte[] rgba, int width, int height)
    {
        return EncodeRgbaToBmp(rgba, width, height);
    }

    private static byte[] EncodeRgbaToBmp(byte[] rgba, int width, int height)
    {
        var rowSize = width * 4;
        var pixelDataSize = rowSize * height;
        var fileSize = 54 + pixelDataSize;

        var bmp = new byte[fileSize];

        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        WriteUInt32LittleEndian(bmp, 2, (uint)fileSize);
        WriteUInt32LittleEndian(bmp, 10, 54u);
        WriteUInt32LittleEndian(bmp, 14, 40u);
        WriteUInt32LittleEndian(bmp, 18, (uint)width);
        WriteUInt32LittleEndian(bmp, 22, (uint)height);
        WriteUInt16LittleEndian(bmp, 26, 1);
        WriteUInt16LittleEndian(bmp, 28, 32);
        WriteUInt32LittleEndian(bmp, 34, (uint)pixelDataSize);

        for (var y = 0; y < height; y++)
        {
            var srcRow = (height - 1 - y) * width * 4;
            var dstRow = 54 + y * rowSize;
            for (var x = 0; x < width; x++)
            {
                bmp[dstRow + x * 4 + 0] = rgba[srcRow + x * 4 + 2];
                bmp[dstRow + x * 4 + 1] = rgba[srcRow + x * 4 + 1];
                bmp[dstRow + x * 4 + 2] = rgba[srcRow + x * 4 + 0];
                bmp[dstRow + x * 4 + 3] = rgba[srcRow + x * 4 + 3];
            }
        }

        return bmp;
    }

    private static void WriteUInt32LittleEndian(byte[] buf, int offset, uint value)
    {
        buf[offset] = (byte)value;
        buf[offset + 1] = (byte)(value >> 8);
        buf[offset + 2] = (byte)(value >> 16);
        buf[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteUInt16LittleEndian(byte[] buf, int offset, ushort value)
    {
        buf[offset] = (byte)value;
        buf[offset + 1] = (byte)(value >> 8);
    }
}
