﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace VKChatThemesV2Demo {
    public class Gradient {
        [JsonProperty("angle")]
        public int Angle { get; set; }

        [JsonProperty("colors")]
        public List<string> Colors { get; set; }
    }

    #region Appearance

    public class AppearanceColors {
        [JsonProperty("bubble_gradient")]
        public Gradient BubbleGradient { get; set; }

        [JsonProperty("accent_color")]
        public string AccentColor { get; set; }

        [JsonProperty("header_tint")]
        public string HeaderTint { get; set; }

        [JsonProperty("write_bar_tint")]
        public string WriteBarTint { get; set; }
    }

    public class Appearance {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("sort")]
        public int Sort { get; set; }

        [JsonProperty("light")]
        public AppearanceColors Light { get; set; }

        [JsonProperty("dark")]
        public AppearanceColors Dark { get; set; }
    }

    #endregion

    #region Background

    public class BackgroundSource {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("width")]
        public double Width { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }
    }

    public class VectorBackgroundSource : BackgroundSource {
        [JsonProperty("opacity")]
        public double Opacity { get; set; }
    }

    public class ColorEllipse {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("radius_x")]
        public double RadiusX { get; set; }

        [JsonProperty("radius_y")]
        public double RadiusY { get; set; }
    }

    public class Blur {
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("opacity")]
        public double Opacity { get; set; }

        [JsonProperty("radius")]
        public double Radius { get; set; }
    }

    public class VectorBackground {
        [JsonProperty("svg")]
        public VectorBackgroundSource SVG { get; set; }

        [JsonProperty("color_ellipses")]
        public List<ColorEllipse> ColorEllipses { get; set; }

        [JsonProperty("gradient")]
        public Gradient Gradient { get; set; }

        [JsonProperty("blur")]
        public Blur Blur { get; set; }
    }

    public class BackgroundSources {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("vector")]
        public VectorBackground Vector { get; set; }

        [JsonProperty("raster")]
        public BackgroundSource Raster { get; set; }
    }

    public class Background {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("sort")]
        public int Sort { get; set; }

        [JsonProperty("light")]
        public BackgroundSources Light { get; set; }

        [JsonProperty("dark")]
        public BackgroundSources Dark { get; set; }
    }

    #endregion

    public class Style {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("sort")]
        public int Sort { get; set; }

        [JsonProperty("appearance_id")]
        public string AppearanceId { get; set; }

        [JsonProperty("background_id")]
        public string BackgroundId { get; set; }

        public override string ToString() {
            return Id;
        }
    }

    public class ConversationStylesV2 {
        [JsonProperty("appearances")]
        public List<Appearance> Appearances { get; set; }

        [JsonProperty("backgrounds")]
        public List<Background> Backgrounds { get; set; }

        [JsonProperty("styles")]
        public List<Style> Styles { get; set; }
    }
}