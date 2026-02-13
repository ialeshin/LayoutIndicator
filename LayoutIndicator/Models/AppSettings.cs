using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LayoutIndicator.Models;

public class AppSettings
{
    [JsonPropertyName("stripWidth")]
    public int StripWidth { get; set; } = 20;

    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 0.7;

    /// <summary>
    /// Display mode: "always" for always-on, "flash" for flash-on-change.
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "always";

    [JsonPropertyName("flashDurationSeconds")]
    public double FlashDurationSeconds { get; set; } = 3.0;

    /// <summary>
    /// Maps keyboard layout culture names (e.g. "en-US", "ru-RU") to hex color strings.
    /// </summary>
    [JsonPropertyName("layouts")]
    public Dictionary<string, string> Layouts { get; set; } = new()
    {
        ["en-US"] = "#0078D4",
        ["ru-RU"] = "#E81123"
    };

    /// <summary>
    /// Color used when the current layout is not found in the Layouts map.
    /// </summary>
    [JsonPropertyName("defaultColor")]
    public string DefaultColor { get; set; } = "#888888";
}
