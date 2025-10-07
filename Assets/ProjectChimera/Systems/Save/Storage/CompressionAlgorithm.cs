using ProjectChimera.Core.Logging;
namespace ProjectChimera.Systems.Save.Storage
{
    /// <summary>
    /// Compression algorithm types for data storage
    /// </summary>
    public enum CompressionAlgorithm
    {
        None,
        GZip,
        Deflate,
        Brotli,
        LZ4,
        LZMA
    }
    
    /// <summary>
    /// Compression level settings
    /// </summary>
    public enum CompressionLevel
    {
        NoCompression,
        Fastest,
        Optimal,
        SmallestSize
    }
}