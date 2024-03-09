using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LoveOfCooking.Objects
{
	public enum Animation
	{
		None,
		Open,
		Close,
		Dropdown,
		Bounce
	}

	public class CookbookAnimation
	{
		// Timer
		private const uint READY = uint.MaxValue;
		private uint _start;

		// Animation
		private Animation _animation;
		private uint _frame;
		private float _fade;
		private bool _isVisible;
		private bool _isPlaying;
		private float _alpha;
		private float _scale;
		private Point _offset;
		private Action _onComplete;
		private static Texture2D _texture;

		// Definitions
		private uint FirstFrame => 0;
		private uint LastFrame => (uint)(CookbookAnimation._texture.Width / CookbookAnimation.Size.X);
		private static uint FrameTime => 4;
		private static uint FastFrameTime => 3;
		private static uint DropTime => 60;
		private static uint BounceTime => 20;
		private static uint FadeTime => 5;
		private static float FadeTo => 0.5f;
		private static readonly Point Size = new(x: 256, y: 256);

		public CookbookAnimation() {}

		public static void Reload(IModHelper helper)
		{
			CookbookAnimation._texture = Game1.content.Load
				<Texture2D>
				(AssetManager.GameContentCookbookSpriteSheetPath);
		}

		public void Register(IModHelper helper)
		{
			helper.Events.GameLoop.UpdateTicked += this.Update;
			helper.Events.Display.RenderedHud += this.Draw;
		}

		public void Play(Animation animation, Action onComplete = null)
		{
			// Set playing flags
			this._isPlaying = true;
			this._isVisible = true;

			// Set general values
			this._alpha = 1;
			this._scale = 1;

			// Set specific values
			this._animation = animation;
			this._onComplete = onComplete;
			this._start = CookbookAnimation.READY;
		}

		public void Hide()
		{
			this._isVisible = false;
			Game1.activeClickableMenu?.exitThisMenu();
		}

		private void Update(object sender, UpdateTickedEventArgs e)
		{
			// Update blackout fade independently of animation
			if (this._isVisible && this._fade < CookbookAnimation.FadeTo)
				this._fade += CookbookAnimation.FadeTo / CookbookAnimation.FadeTime;
			else if (!this._isVisible && this._fade > 0)
				this._fade -= CookbookAnimation.FadeTo / CookbookAnimation.FadeTime;

			// Skip animation updates if not playing
			if (!this._isPlaying)
				return;

			void end()
			{
				this._isPlaying = false;
				this._onComplete?.Invoke();
			}

			// Start animating when ready value set
			if (this._start == CookbookAnimation.READY)
			{
				this._start = e.Ticks;
				this._frame = this._animation switch
				{
					Animation.Open => this.LastFrame - 1,
					Animation.Close => this.FirstFrame,
					Animation.Dropdown => this.LastFrame - 1,
					Animation.Bounce => this.LastFrame - 1,
					_ => this.FirstFrame
				};
			}

			uint elapsed = e.Ticks - this._start;
			float ratio;
			float inverse;

			int x = -CookbookAnimation.Size.X / 4 * Game1.pixelZoom;
			int y = -CookbookAnimation.Size.Y / 7 * Game1.pixelZoom;

			switch (this._animation)
			{
				case Animation.Open:
				{
					// End animation on first frame
					if (this._frame <= this.FirstFrame)
					{
						end();
						return;
					}

					ratio = elapsed / ((float)CookbookAnimation.FrameTime * (this.LastFrame - 1));
					inverse = 1 - ratio;

					// Update frame based on frame time and elapsed time
					this._frame = this.LastFrame - 1 - (elapsed / CookbookAnimation.FrameTime);

					// Update position based on elapsed time
					this._offset = new Point(
						x: (int)(x * Math.Cos(ratio * Math.PI / 2)),
						y: (int)(y * ratio));

					break;
				}
				case Animation.Close:
				{
					// End animation on last frame
					if (this._frame >= this.LastFrame - 1)
					{
						end();
						this.Hide();
						return;
					}

					ratio = elapsed / (float)(CookbookAnimation.FastFrameTime * (this.LastFrame - 1));
					inverse = 1 - ratio;

					// Update frame based on frame time and elapsed time
					this._frame = elapsed / CookbookAnimation.FastFrameTime;

					// Update position based on elapsed time
					this._offset = new Point(
						x: (int)(x * Math.Cos(inverse * Math.PI / 2)),
						y: (int)(y * inverse));

					// ModEntry.Instance.Monitor.Log($"Ratio: {ratio}, Inverse: {inverse}", LogLevel.Debug);

					break;
				}
				case Animation.Dropdown:
				{
					// End animation on duration elapsed
					if (elapsed > CookbookAnimation.DropTime)
					{
						end();
						return;
					}

					ratio = elapsed / (float)CookbookAnimation.DropTime;
					inverse = 1 - ratio;

					// Update position based on elapsed time
					this._offset = new Point(
						x: x,
						y: (int)(-CookbookAnimation.Size.Y * 1.25f * Game1.pixelZoom * Math.Sin(inverse * Math.PI) / 2));

					// Fade in animation
					this._alpha = ratio * 2 - 0.5f;
					this._scale = (float)(1 + Math.Sin(ratio * Math.PI) / 2);

					break;
				}
				case Animation.Bounce:
				{
					// End animation on duration elapsed
					if (elapsed > CookbookAnimation.BounceTime)
					{
						end();
						return;
					}

					ratio = elapsed / (float)CookbookAnimation.BounceTime;
					inverse = 1 - ratio;

					// Scale animation based on elapsed time
					this._scale = (float)(1 + Math.Sin(ratio * Math.PI) / 26);

					// Update position based on elapsed time
					this._offset = new Point(
						x: x,
						y: (int)(-CookbookAnimation.Size.Y * 0.025f * Game1.pixelZoom * Math.Sin(ratio * Math.PI)));

					break;
				}
			}
		}

		private void Draw(object sender, RenderedHudEventArgs e)
		{
			if (this._fade <= 0 || CookbookAnimation._texture == null)
				return;

			Rectangle area = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea;

			// Blackout
			e.SpriteBatch.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: area,
				color: Color.Black * this._fade);

			// Animation
			e.SpriteBatch.Draw(
				texture: CookbookAnimation._texture,
				position: area.Center.ToVector2() + this._offset.ToVector2() * this._scale,
				sourceRectangle: new(
					x: CookbookAnimation.Size.X * (int)this._frame,
					y: 0,
					width: CookbookAnimation.Size.X,
					height: CookbookAnimation.Size.Y),
				color: Color.White * this._alpha * (this._fade / CookbookAnimation.FadeTo),
				rotation: 0,
				origin: CookbookAnimation.Size.ToVector2() / 2,
				scale: Game1.pixelZoom * this._scale,
				effects: SpriteEffects.None,
				layerDepth: 1);
		}
	}
}
