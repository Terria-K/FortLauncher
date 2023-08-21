namespace FortLauncher;

public class StringProgress : IProgress<long>
{
    public long Value;

    public string Percentage 
    {
        get 
        {
            float val = ((float)Value / (float)TotalBytes) * 100;
            return $"{Math.Floor(val)}/100";
        }
    }

    public string Bytes
    {
        get 
        {
            float value = ((float)Value / 1024f) / 1024f;
            float totalBytes = ((float)TotalBytes / 1024f) / 1024f;
            return $"{value.ToString("0.00")} MB/{totalBytes.ToString("0.00")} MB";
        }
    }

    public long TotalBytes;


    public void Report(long value)
    {
        Value = value;
    }

    public void Reset() 
    {
        Value = 0;
        TotalBytes = 0;
    }
}