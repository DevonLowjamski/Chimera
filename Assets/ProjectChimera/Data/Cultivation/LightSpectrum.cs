using UnityEngine;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Enumeration of light spectrum types used in cannabis cultivation.
    /// Different spectrums affect plant growth, flowering, and cannabinoid production.
    /// </summary>
    public enum LightSpectrum
    {
        // Individual color spectrums
        Red,                    // Pure red spectrum
        Blue,                   // Pure blue spectrum  
        Green,                  // Pure green spectrum
        FarRed,                 // Far-red spectrum
        UVA,                    // UV-A spectrum
        UVB,                    // UV-B spectrum
        WhiteBalance,           // White balance spectrum
        
        // Full spectrum options
        FullSpectrum,           // Complete spectrum including UV and IR
        SunlightSpectrum,       // Natural sunlight spectrum
        
        // Vegetative growth spectrums
        BlueRich,               // High blue content for vegetative growth
        CoolWhite,              // 4000K-6500K for vegetative stage
        MetalHalide,            // Traditional MH spectrum
        
        // Flowering spectrums
        RedRich,                // High red content for flowering
        WarmWhite,              // 2700K-3500K for flowering stage
        HighPressureSodium,     // Traditional HPS spectrum
        
        // Specialized spectrums
        UVEnhanced,             // Enhanced UV for trichome production
        FarRedEnhanced,         // Enhanced far-red for stem elongation
        PurpleSpectrum,         // Blue + Red LED combination
        
        // Advanced LED spectrums
        QuantumBoard,           // Modern full-spectrum LED
        COBSpectrum,            // Chip-on-board LED spectrum
        CustomSpectrum          // User-defined spectrum
    }
} 