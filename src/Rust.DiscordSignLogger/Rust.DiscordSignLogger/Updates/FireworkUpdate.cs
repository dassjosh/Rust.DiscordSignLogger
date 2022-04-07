using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Facepunch;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Plugins;
using Color = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;
using Star = ProtoBuf.PatternFirework.Star;

namespace Rust.SignLogger.Updates
{
    public class FireworkUpdate : BaseImageUpdate
    {
        public override bool SupportsTextureIndex => false;
        public PatternFirework Firework => (PatternFirework)Entity;

        public FireworkUpdate(BasePlayer player, PatternFirework entity, List<SignMessage> messages) : base(player, entity, messages, false)
        {
            
        }

        public override byte[] GetImage()
        {
            PatternFirework firework = Firework;
            List<Star> stars = firework.Design.stars;
            
            using (Bitmap image = new Bitmap(DiscordSignLogger.Instance.FireworkImageSize, DiscordSignLogger.Instance.FireworkImageSize))
            {
                using (Graphics g = Graphics.FromImage(image))
                {
                    for (int index = 0; index < stars.Count; index++)
                    {
                        Star star = stars[index];
                        int x = (int)((star.position.x + 1) * DiscordSignLogger.Instance.FireworkHalfImageSize);
                        int y = (int)((-star.position.y + 1) * DiscordSignLogger.Instance.FireworkHalfImageSize);
                        g.FillEllipse(GetBrush(star.color), x, y, DiscordSignLogger.Instance.FireworkCircleSize, DiscordSignLogger.Instance.FireworkCircleSize);
                    }

                    return GetImageBytes(image);
                }
            }
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
            MemoryStream stream = Pool.Get<MemoryStream>();
            image.Save(stream, ImageFormat.Png);
            byte[] bytes = stream.ToArray();
            Pool.FreeMemoryStream(ref stream);
            return bytes;
        }
    }
}