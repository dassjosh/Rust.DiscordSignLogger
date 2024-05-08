using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Rust.SignLogger.Plugins;
using Color = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;
using Star = ProtoBuf.PatternFirework.Star;

namespace Rust.SignLogger.Updates;

public class FireworkUpdate : BaseImageUpdate
{
    public PatternFirework Firework => (PatternFirework)Entity;

    public FireworkUpdate(BasePlayer player, PatternFirework entity) : base(player, entity, false) { }

    public override byte[] GetImage()
    {
        PatternFirework firework = Firework;
        List<Star> stars = firework.Design.stars;

        using Bitmap image = new(DiscordSignLogger.Instance.FireworkImageSize, DiscordSignLogger.Instance.FireworkImageSize);
        using Graphics g = Graphics.FromImage(image);
        for (int index = 0; index < stars.Count; index++)
        {
            Star star = stars[index];
            int x = (int)((star.position.x + 1) * DiscordSignLogger.Instance.FireworkHalfImageSize);
            int y = (int)((-star.position.y + 1) * DiscordSignLogger.Instance.FireworkHalfImageSize);
            g.FillEllipse(GetBrush(star.color), x, y, DiscordSignLogger.Instance.FireworkCircleSize, DiscordSignLogger.Instance.FireworkCircleSize);
        }

        return GetImageBytes(image);
    }
        
    private Brush GetBrush(UnityEngine.Color color)
    {
        Brush brush = DiscordSignLogger.Instance.FireworkBrushes[color];
        if (brush == null)
        {
            brush = new SolidBrush(FromUnityColor(color));
            DiscordSignLogger.Instance.FireworkBrushes[color] = brush;
        }

        return brush;
    }
        
    private Color FromUnityColor(UnityEngine.Color color)
    {
        int red = FromUnityColorField(color.r);
        int green = FromUnityColorField(color.g);
        int blue = FromUnityColorField(color.b);
        int alpha = FromUnityColorField(color.a);

        return Color.FromArgb(alpha, red, green, blue);
    }
        
    private int FromUnityColorField(float color)
    {
        return (int)(color * 255);
    }
        
    private byte[] GetImageBytes(Bitmap image)
    {
        MemoryStream stream = DiscordSignLogger.Instance.Pool.GetMemoryStream();
        image.Save(stream, ImageFormat.Png);
        byte[] bytes = stream.ToArray();
        DiscordSignLogger.Instance.Pool.FreeMemoryStream(stream);
        return bytes;
    }
}