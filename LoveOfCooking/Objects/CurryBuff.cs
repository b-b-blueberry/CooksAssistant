using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace LoveOfCooking.Objects
{
	public class CurryBuff : Buff
	{
		/// <summary>
		/// Region in world where monsters will be damaged.
		/// </summary>
		public Rectangle DamageArea { get; private set; }
		/// <summary>
		/// Delay in milliseconds between fire puffs.
		/// </summary>
		public int Delay { get; private set; } = 135;
		/// <summary>
		/// Timer counting down milliseconds towards next fire puff.
		/// </summary>
		private float _countdown;
		/// <summary>
		/// Number counting up fire puffs.
		/// </summary>
		private int _fireCount = 0;
		/// <summary>
		/// Goal number of fire puffs before resetting.
		/// </summary>
		private readonly int _fireCountTo = 5;
		/// <summary>
		/// Asset key for fire puff spritesheet.
		/// </summary>
		private readonly string _fireTexture = "TileSheets/Projectiles";
		/// <summary>
		/// Source area of fire puff in spritesheet.
		/// </summary>
		private readonly Rectangle _fireTextureRegion = new(x: 32, y: 16, width: 16, height: 16);

		public CurryBuff(Buff buff) : base(id: buff.id) {}

		public override bool update(GameTime time)
		{
			this._countdown -= time.ElapsedGameTime.Milliseconds;
			bool isBlocked = Game1.currentLocation.currentEvent is not null;
			if (this._countdown <= 0 && !isBlocked)
			{
				// Restart timer
				this._countdown = this.Delay;

				// Fire effects
				float speed = 8f;
				float spread = 0.5f;
				float maxSpread = spread * this._fireCountTo;
				Vector2 motion = Game1.player.FacingDirection switch
				{
					Game1.left => new Vector2(x: -0.5f, y: 0),
					Game1.right => new Vector2(x: 1, y: 0),
					Game1.up => new Vector2(x: 0, y: -0.5f),
					Game1.down => new Vector2(x: 0, y: 1),
					_ => Vector2.Zero
				};
				Vector2 offset = Game1.player.FacingDirection switch
				{
					Game1.up => new Vector2(x: 6, y: -24),
					Game1.right => new Vector2(x: 12, y: -14),
					Game1.down => new Vector2(x: 6, y: -12),
					Game1.left => new Vector2(x: 0, y: -14),
					_ => Vector2.Zero
				};
				Game1.Multiplayer.broadcastSprites(
					location: Game1.player.currentLocation,
					sprites: new TemporaryAnimatedSprite(
						textureName: this._fireTexture,
						sourceRect: this._fireTextureRegion,
						animationInterval: 300f,
						animationLength: 1,
						numberOfLoops: 0,
						position: offset * Game1.pixelZoom,
						flicker: false,
						flipped: false)
					{
						scale = 1f,
						scaleChange = 0.25f,
						delayBeforeAnimationStart = 1,
						motion = motion * new Vector2(value: speed)
							+ new Vector2(value: -maxSpread) / 2 * Game1.pixelZoom
							+ new Vector2(x: Math.Abs(motion.Y) * this._fireCount * spread, y: Math.Abs(motion.X) * this._fireCount * spread) * Game1.pixelZoom,
						rotationChange = (float)(5d * Game1.random.NextDouble() * (float)Math.PI / 64d),
						layerDepth = 1f,
						alphaFade = 0.05f,
						positionFollowsAttachedCharacter = true,
						attachedCharacter = Game1.player
					});

				if (++this._fireCount >= this._fireCountTo)
				{
					// Update damage area
					motion = Game1.player.FacingDirection switch
					{
						Game1.up => new Vector2(x: 0, y: -1),
						Game1.right => new Vector2(x: 1, y: 0),
						Game1.down => new Vector2(x: 0, y: 1),
						Game1.left => new Vector2(x: -1, y: 0),
						_ => Vector2.Zero
					};
					Point size = ModEntry.ItemDefinitions.CurryBuffArea;
					Point centre = Game1.player.getStandingPosition().ToPoint();
					centre.Y -= Game1.tileSize / 4 * 3;
					Point distance = new Point(
						x: (int)(-size.X / 2 + motion.X * Game1.tileSize),
						y: (int)(-size.Y / 2 + motion.Y * Game1.tileSize));
					this.DamageArea = new(
						location: centre + distance,
						size: size);

					// Damage monsters
					Game1.currentLocation.damageMonster(
						areaOfEffect: this.DamageArea,
						minDamage: ModEntry.ItemDefinitions.CurryBuffDamage.X,
						maxDamage: ModEntry.ItemDefinitions.CurryBuffDamage.Y,
						isBomb: false,
						knockBackModifier: ModEntry.ItemDefinitions.CurryBuffKnockbackMultiplier,
						addedPrecision: 0,
						critChance: 0f,
						critMultiplier: 0f,
						triggerMonsterInvincibleTimer: true,
						isProjectile: true,
						who: Game1.player);

					this._fireCount = 0;
				}
			}

			// Defer ready-to-end behaviours to base class
			return base.update(time);
		}

		public override void OnAdded()
		{
			Utils.PlayFoodBurnEffects(burntQuantity: 10);
			Game1.playSound("furnace");
		}

		public override void OnRemoved()
		{
			Utils.PlayFoodBurnEffects(burntQuantity: 10);
			Game1.playSound("fireball");
		}
	}
}
