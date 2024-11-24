using System;
using LoveOfCooking.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace LoveOfCooking.Objects
{
	public class CoinDebris : Debris
	{
		/// <summary>
		/// Money earned when coin debris is collected by player.
		/// </summary>
		public int CoinValue { get; set; }
		/// <summary>
		/// Number of coins created as debris.
		/// </summary>
		public int CoinCount { get; set; }
		/// <summary>
		/// Spritesheet used for custom draw behaviour.
		/// </summary>
		public Texture2D Texture { get; set; }
		/// <summary>
		/// Source area of first animation frame in spritesheet.
		/// </summary>
		public Rectangle TextureRegion { get; set; }
		/// <summary>
		/// Number of animation frames in spritesheet.
		/// </summary>
		public int AnimFrames { get; set; }
		/// <summary>
		/// Current animation frame used for custom draw behaviour.
		/// </summary>
		private int _animFrame;

		public CoinDebris(int value, int count, Vector2 position, Farmer farmer) : base()
		{
			// Debris
			this.debrisType.Set(DebrisType.CHUNKS); // ...
			this.chunkType.Set(8); // Somehow allows us to collect debris

			this.InitializeChunks(
				numberOfChunks: count,
				debrisOrigin: position,
				playerPosition: farmer.Position);

			foreach (Chunk chunk in this.Chunks)
			{
				chunk.xVelocity.Value = Game1.random.Next(-1, 2);
				chunk.xVelocity.Value *= 1.5f;
				chunk.yVelocity.Value *= 1.5f;
			}

			this.chunksColor.Set(Color.Transparent); // Hides default draw behaviour

			// CoinDebris
			this.CoinValue = value;

			this.Texture = ModEntry.SpriteSheet;
			this.TextureRegion = CookingMenu.CoinSmallSource;
			this.AnimFrames = 10;

			this._animFrame = Game1.random.Next(this.AnimFrames);
		}

		public override bool isEssentialItem()
		{
			// Will not fly to player, can be lost in water
			return false;
		}

		public override bool collect(Farmer farmer, Chunk chunk = null)
		{
			// Award money to player when collected
			Game1.playSound("moneyDial");
			farmer.Money += this.CoinValue;
			return true;
		}

		/// <summary>
		/// Unique animated coin debris draw behaviour.
		/// </summary>
		public void Draw(SpriteBatch b, GameLocation location)
		{
			this._animFrame = (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100 % this.AnimFrames);

			Texture2D texture;
			Vector2 position;
			Rectangle source;
			float drawLayer;
			float scale;
			for (int i = 0; i < this.Chunks.Count; ++i)
			{
				int frame = (i * 3 + this._animFrame) % this.AnimFrames;

				// shadow
				texture = Game1.shadowTexture;
				source = Game1.shadowTexture.Bounds;
				position = Utility.snapDrawPosition(
						Game1.GlobalToLocal(
							viewport: Game1.viewport,
							globalPosition: new Vector2(
								x: this.Chunks[i].position.X + 16f,
								y: (this.chunksMoveTowardPlayer ? this.Chunks[i].position.Y : this.chunkFinalYLevel) + 32f)));
				scale = Math.Min(3f, 3f - (this.chunksMoveTowardPlayer ? 0f : ((this.chunkFinalYLevel - this.Chunks[i].position.Y) / 96f)));
				drawLayer = this.chunkFinalYLevel / 10000f;
				b.Draw(
					texture: texture,
					position: position,
					sourceRectangle: source,
					color: Color.White * 0.75f,
					rotation: 0f,
					origin: Game1.shadowTexture.Bounds.Size.ToVector2() / 2,
					scale: scale,
					effects: SpriteEffects.None,
					layerDepth: drawLayer);

				// coin
				texture = this.Texture;
				source = this.TextureRegion;
				source.X += frame * this.TextureRegion.Width;
				position = Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, this.Chunks[i].position.Value));
				scale = 4f * this.scale.Value;
				drawLayer = (this.Chunks[i].position.Y + 128f + this.Chunks[i].position.X / 10000f) / 10000f;
				b.Draw(
					texture: texture,
					position: position,
					sourceRectangle: source,
					color: Color.White,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: scale,
					effects: SpriteEffects.None,
					layerDepth: drawLayer);
			}
		}
	}
}
