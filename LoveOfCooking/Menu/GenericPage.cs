using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static LoveOfCooking.Menu.CookingMenu;
using static StardewValley.LocalizedContentManager;

namespace LoveOfCooking.Menu
{
	public abstract class GenericPage : CookingMenuSubMenu
    {
        public bool IsVisible { get; set; }
        public int Margin { get; private set; }
        public Rectangle ContentArea { get; private set; }
        public bool IsLeftSide { get; protected set; }

        protected int _lineWidth;
        protected int _textWidth;
        protected Vector2 _textScale;
        protected Point _offset;

		protected enum TextJustify
		{
			Left = 0,
			Centre = 1,
			Right = 2
		}

		public abstract ClickableComponent DefaultClickableComponent { get; }

        public abstract bool TryPop();

        public GenericPage(CookingMenu menu) : base(menu: menu) { }

        public override void LayoutComponents(Rectangle area)
        {
            base.LayoutComponents(area: area);

			this.Margin = this.IsLeftSide ? MarginLeft : MarginRight;

			this.ContentArea = new(
                x: this.Area.X + this.Margin,
                y: this.Area.Y,
                width: this.Area.Width - this.Margin,
                height: this.Area.Height);

			this._lineWidth = this.ContentArea.Width - 12 * Scale;
			this._textWidth = this._lineWidth + TextMuffinTopOverDivider * 2;
            this._textScale = CurrentLanguageCode is LanguageCode.ko && ModEntry.Config.ResizeKoreanFonts
                ? ModEntry.Definitions.KoreanFontScale
                : Vector2.One;
		}

        protected void DrawText(SpriteBatch b, string text, float x, float y, float w = -1, float scale = 1f, SpriteFont font = null, TextJustify justify = TextJustify.Left, Color? colour = null, bool absolute = false)
        {
            font ??= Game1.smallFont;
            Point position = absolute ? Point.Zero : this.ContentArea.Location;
            position.Y -= this.Menu.yPositionOnScreen;

            // Adjust text position
            w = w > 0 ? w : 999999999;
            position.X -= (int)(font.MeasureString(text).X * ((int)justify * 0.5f));
            if (CurrentLanguageCode is LanguageCode.ko && ModEntry.Config.ResizeKoreanFonts)
                scale *= ModEntry.Definitions.KoreanFontScale.Y;

            // Draw text
            Utility.drawTextWithShadow(b,
                text: Game1.parseText(
                    text: text,
                    whichFont: font,
                    width: (int)w),
                font: font,
                position: new(
                    x: position.X + x,
                    y: position.Y + y),
                color: colour ?? TextColour,
                scale: scale);
        }

        internal void DrawHorizontalDivider(SpriteBatch b, float x, float y, int w)
        {
            Point position = this.ContentArea.Location;
            position.Y -= this.Menu.yPositionOnScreen;
            Utility.drawLineWithScreenCoordinates(
                x1: (int)(position.X + x) + TextMuffinTopOverDivider,
                y1: (int)(position.Y + y),
                x2: (int)(position.X + x) + w + TextMuffinTopOverDivider,
                y2: (int)(position.Y + y),
                b: b,
                color1: ModEntry.Definitions.CookingMenuDividerColour);
        }
    }
}
