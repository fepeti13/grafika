using Silk.NET.Maths;

namespace PirateShootingGame
{
    internal class GameState
    {
        public Player Player { get; private set; }
        public List<Pirate> Pirates { get; private set; }
        public List<Bullet> Bullets { get; private set; }
        public List<Vector3D<float>> Houses { get; private set; }
        public int PiratesDefeated { get; private set; }

        private Random random = new Random();
        private float bulletCooldown = 0f;
        private const float BulletCooldownTime = 0.3f;

        public GameState()
        {
            Player = new Player();
            Pirates = new List<Pirate>();
            Bullets = new List<Bullet>();
            Houses = new List<Vector3D<float>>();
        }

        public void Initialize()
        {
            Player.Position = new Vector3D<float>(0f, 0f, 0f);
            Player.Rotation = 0f;

            Pirates.Clear();
            Bullets.Clear();
            Houses.Clear();
            PiratesDefeated = 0;

            // Spawn pirates randomly around the field
            for (int i = 0; i < 8; i++)
            {
                Pirates.Add(new Pirate
                {
                    Position = new Vector3D<float>(
                        random.NextSingle() * 40f - 20f,
                        0f,
                        random.NextSingle() * 40f - 20f
                    ),
                    Rotation = random.NextSingle() * MathF.PI * 2f,
                    IsAlive = true
                });
            }

            // Place some houses for decoration
            for (int i = 0; i < 5; i++)
            {
                Houses.Add(new Vector3D<float>(
                    random.NextSingle() * 30f - 15f,
                    0f,
                    random.NextSingle() * 30f - 15f
                ));
            }
        }

        public void Update(float deltaTime)
        {
            if (bulletCooldown > 0)
                bulletCooldown -= deltaTime;

            // Update bullets
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                var bullet = Bullets[i];
                bullet.Update(deltaTime);

                // Remove bullets that are too far or inactive
                if (!bullet.IsActive || Vector3D.Distance(bullet.Position, Player.Position) > 50f)
                {
                    Bullets.RemoveAt(i);
                    continue;
                }

                // Check collision with pirates
                foreach (var pirate in Pirates.Where(p => p.IsAlive))
                {
                    if (Vector3D.Distance(new Vector3D<float>(bullet.Position.X, 0f, bullet.Position.Z), 
                                         new Vector3D<float>(pirate.Position.X, 0f, pirate.Position.Z)) < 1f)
                    {
                        pirate.IsAlive = false;
                        bullet.IsActive = false;
                        PiratesDefeated++;
                        break;
                    }
                }
            }

            // Update pirates (simple AI - move toward player slowly)
            foreach (var pirate in Pirates.Where(p => p.IsAlive))
            {
                pirate.Update(deltaTime, Player.Position);
            }
        }

        public void ShootBullet()
        {
            if (bulletCooldown > 0) return;

            var bullet = new Bullet
            {
                Position = new Vector3D<float>(Player.Position.X, 0.5f, Player.Position.Z),
                Velocity = new Vector3D<float>(
                    MathF.Sin(Player.Rotation) * 15f,
                    0f,
                    MathF.Cos(Player.Rotation) * 15f
                ),
                IsActive = true
            };

            Bullets.Add(bullet);
            bulletCooldown = BulletCooldownTime;
        }

        public void RestartGame()
        {
            Initialize();
        }
    }

    internal class Player
    {
        public Vector3D<float> Position { get; set; }
        public float Rotation { get; set; }

        private const float MoveSpeed = 8f;
        private const float TurnSpeed = 3f;

        public void MoveForward()
        {
            Position += new Vector3D<float>(
                MathF.Sin(Rotation) * MoveSpeed * 0.1f,
                0f,
                MathF.Cos(Rotation) * MoveSpeed * 0.1f
            );
            
            // Keep player in bounds
            Position = new Vector3D<float>(
                MathF.Max(-25f, MathF.Min(25f, Position.X)),
                Position.Y,
                MathF.Max(-25f, MathF.Min(25f, Position.Z))
            );
        }

        public void MoveBackward()
        {
            Position -= new Vector3D<float>(
                MathF.Sin(Rotation) * MoveSpeed * 0.1f,
                0f,
                MathF.Cos(Rotation) * MoveSpeed * 0.1f
            );
            
            // Keep player in bounds
            Position = new Vector3D<float>(
                MathF.Max(-25f, MathF.Min(25f, Position.X)),
                Position.Y,
                MathF.Max(-25f, MathF.Min(25f, Position.Z))
            );
        }

        public void TurnLeft()
        {
            Rotation -= TurnSpeed * 0.1f;
        }

        public void TurnRight()
        {
            Rotation += TurnSpeed * 0.1f;
        }
    }

    internal class Pirate
    {
        public Vector3D<float> Position { get; set; }
        public float Rotation { get; set; }
        public bool IsAlive { get; set; }

        private const float MoveSpeed = 2f;
        private float moveTimer = 0f;
        private Vector2D<float> moveDirection;

        public void Update(float deltaTime, Vector3D<float> playerPosition)
        {
            if (!IsAlive) return;

            moveTimer += deltaTime;

            // Change direction every 2 seconds or move toward player occasionally
            if (moveTimer >= 2f || moveDirection == Vector2D<float>.Zero)
            {
                moveTimer = 0f;
                
                // 30% chance to move toward player, 70% chance to move randomly
                if (new Random().NextSingle() < 0.3f)
                {
                    var directionToPlayer = Vector2D.Normalize(new Vector2D<float>(playerPosition.X - Position.X, playerPosition.Z - Position.Z));
                    moveDirection = directionToPlayer;
                }
                else
                {
                    var random = new Random();
                    moveDirection = new Vector2D<float>(
                        random.NextSingle() * 2f - 1f,
                        random.NextSingle() * 2f - 1f
                    );
                    moveDirection = Vector2D.Normalize(moveDirection);
                }

                // Update rotation to face movement direction
                Rotation = MathF.Atan2(moveDirection.X, moveDirection.Y);
            }

            // Move the pirate
            Position += new Vector3D<float>(moveDirection.X * MoveSpeed * deltaTime, 0f, moveDirection.Y * MoveSpeed * deltaTime);

            // Keep pirates in bounds
            Position = new Vector3D<float>(
                MathF.Max(-30f, MathF.Min(30f, Position.X)),
                Position.Y,
                MathF.Max(-30f, MathF.Min(30f, Position.Z))
            );
        }
    }

    internal class Bullet
    {
        public Vector3D<float> Position { get; set; }
        public Vector3D<float> Velocity { get; set; }
        public bool IsActive { get; set; }

        private float lifeTime = 0f;
        private const float MaxLifeTime = 5f;

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            Position += Velocity * deltaTime;
            lifeTime += deltaTime;

            if (lifeTime >= MaxLifeTime)
            {
                IsActive = false;
            }
        }
    }
}