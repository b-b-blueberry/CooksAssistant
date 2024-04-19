using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;

namespace LoveOfCooking.Objects
{
	public class LasagnaBuff : Buff
	{
		/// <summary>
		/// Delay in milliseconds between buff effects applied to monsters to save performance.
		/// </summary>
		public int Delay { get; private set; } = 300;
		/// <summary>
		/// Delay in counters between animated sprites on affected monsters.
		/// </summary>
		public int SpriteDelay { get; private set; } = 3;
		/// <summary>
		/// Timer counting down milliseconds towards buff effects applied.
		/// </summary>
		private float _countdown;
		/// <summary>
		/// Timer counting down effects applied before creating animated sprites.
		/// </summary>
		private int _spriteCountdown;

		public LasagnaBuff(Buff buff) : base(id: buff.id, source: buff.source, displaySource: buff.displaySource) {}

		public List<Monster> GetTargets()
		{
			return Game1.currentLocation.characters.Where(npc =>
				npc is Monster monster
				&& npc is not (GreenSlime or BigSlime or LavaLurk or Duggy)
				&& monster.IsMonster
				&& monster.Health > 0
				&& !monster.IsInvisible
				&& !monster.isInvincible()
				&& !monster.isGlider.Value
				&& monster.IsWalkingTowardPlayer)?.Cast<Monster>()?.ToList() ?? new();
		}

		public override bool update(GameTime time)
		{
			this._countdown -= time.ElapsedGameTime.Milliseconds;
			bool isBlocked = Game1.currentLocation.currentEvent is not null;
            if (this._countdown <= 0 && !isBlocked)
			{
				// Restart timer
				this._countdown = this.Delay;

				// Continue sprite countdown
				--this._spriteCountdown;
				if (this._spriteCountdown <= 0)
					this._spriteCountdown = this.SpriteDelay;

				// Apply slow effect to monsters
				// Due to how bouncing and flying enemies work, this effect only applies to walking enemies
				int maxSlow = ModEntry.Definitions.LasagnaBuffMaxSlow;
				int minSpeed = ModEntry.Definitions.LasagnaBuffMinSpeed;
				var monsters = this.GetTargets();
				foreach (Monster monster in monsters)
				{
					if (monster is null || monster.currentLocation is null)
						continue;

					// Reduce speed by up to some maximum value, and to a minimum of 1
					for (int i = 0; i <= maxSlow && monster.Speed - i >= minSpeed; ++i)
					{
						monster.addedSpeed = -i;
					}

					// Create visual effects on monster
					if (this._spriteCountdown == this.SpriteDelay)
					{
						TemporaryAnimatedSprite sprite = new(
							textureName: "LooseSprites/Cursors",
							sourceRect: new(359, 1437, 14, 14),
							position: Vector2.Zero,
							flipped: false,
							alphaFade: 0.01f,
							color: Color.GreenYellow)
						{
							alpha = (float)(Game1.random.NextDouble() / 3 + 0.5f),
							xPeriodic = true,
							xPeriodicLoopTime = Game1.random.Next(2000, 3000),
							xPeriodicRange = Game1.random.Next(-32, 32),
							motion = new Vector2(0f, -1f),
							rotationChange = (float)(Math.PI / Game1.random.Next(32, 64)),
							positionFollowsAttachedCharacter = true,
							attachedCharacter = monster,
							layerDepth = 1f,
							scaleChange = 0.04f,
							scaleChangeChange = -0.0008f,
							scale = (float)(2 + Game1.random.NextDouble())
						};
						Game1.Multiplayer.broadcastSprites(location: monster.currentLocation, sprites: sprite);
					}
				}
            }

            // Defer ready-to-end behaviours to base class
            return base.update(time);
		}

		public override void OnAdded()
		{
			base.OnAdded();
		}

		public override void OnRemoved()
		{
			// Reset enemy speed on buff lost
			var monsters = this.GetTargets();
			foreach (Monster monster in monsters)
			{
				if (monster is null || monster.currentLocation is null)
					continue;

				monster.addedSpeed = 0;
			}

			base.OnRemoved();
		}
	}
}
