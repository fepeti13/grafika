using Silk.NET.Maths;

namespace PirateShootingGame
{
    internal class GameState
    {
        public Player Player { get; private set; }
        public List<Pirate> Pirates { get; private set; }
        public List<Bullet> Bullets { get; private set; }
        public List<Vector3D<float>> Trees { get; private set; }
        public int PiratesDefeated { get; private set; }
        public bool IsFirstPersonCamera { get; set; } = false; 

        private Random random = new Random();
        private float bulletCooldown = 0f;
        private const float BulletCooldownTime = 0.3f;

        public GameState()
        {
            Player = new Player();
            Pirates = new List<Pirate>();
            Bullets = new List<Bullet>();
            Trees = new List<Vector3D<float>>();
        }

        public void Initialize()
        {
            Player.Position = new Vector3D<float>(0f, 0f, 0f);
            Player.Rotation = 0f;

            Pirates.Clear();
            Bullets.Clear();
            Trees.Clear();
            PiratesDefeated = 0;

            
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

            
            for (int i = 0; i < 20; i++)  
            {
                Vector3D<float> treePosition;
                bool validPosition;
                int attempts = 0;
                
                do
                {
                    validPosition = true;
                    treePosition = new Vector3D<float>(
                        random.NextSingle() * 35f - 17.5f,
                        0f,
                        random.NextSingle() * 35f - 17.5f
                    );
                    
                    
                    if (Vector3D.Distance(treePosition, Vector3D<float>.Zero) < 3f)
                    {
                        validPosition = false;
                    }
                    
                    
                    foreach (var existingTree in Trees)
                    {
                        if (Vector3D.Distance(treePosition, existingTree) < 2f)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                    
                    attempts++;
                } while (!validPosition && attempts < 50); 
                
                if (validPosition)
                {
                    Trees.Add(treePosition);
                }
            }
        }

        public void Update(float deltaTime)
        {
            if (bulletCooldown > 0)
                bulletCooldown -= deltaTime;

            
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                var bullet = Bullets[i];
                bullet.Update(deltaTime);

                
                if (!bullet.IsActive || Vector3D.Distance(bullet.Position, Player.Position) > 50f)
                {
                    Bullets.RemoveAt(i);
                    continue;
                }

                
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

            
            foreach (var pirate in Pirates.Where(p => p.IsAlive))
            {
                pirate.Update(deltaTime, Player.Position, this);
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

        public bool IsPositionBlocked(Vector3D<float> position, float radius = 0.8f)
        {
            
            foreach (var tree in Trees)
            {
                var distance = Vector3D.Distance(new Vector3D<float>(position.X, 0f, position.Z), 
                                                new Vector3D<float>(tree.X, 0f, tree.Z));
                if (distance < radius)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsPositionBlocked2D(Vector2D<float> position, float radius = 0.8f)
        {
            
            foreach (var tree in Trees)
            {
                var distance = Vector2D.Distance(position, new Vector2D<float>(tree.X, tree.Z));
                if (distance < radius)
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class Player
    {
        public Vector3D<float> Position { get; set; }
        public float Rotation { get; set; }

        private const float MoveSpeed = 8f;
        private const float TurnSpeed = 1.5f;  

        public void MoveForward(GameState gameState)
        {
            var newPosition = Position + new Vector3D<float>(
                MathF.Sin(Rotation) * MoveSpeed * 0.1f,
                0f,
                MathF.Cos(Rotation) * MoveSpeed * 0.1f
            );
            
            
            newPosition = new Vector3D<float>(
                MathF.Max(-25f, MathF.Min(25f, newPosition.X)),
                newPosition.Y,
                MathF.Max(-25f, MathF.Min(25f, newPosition.Z))
            );

            
            if (!gameState.IsPositionBlocked(newPosition))
            {
                Position = newPosition;
            }
        }

        public void MoveBackward(GameState gameState)
        {
            var newPosition = Position - new Vector3D<float>(
                MathF.Sin(Rotation) * MoveSpeed * 0.1f,
                0f,
                MathF.Cos(Rotation) * MoveSpeed * 0.1f
            );
            
            
            newPosition = new Vector3D<float>(
                MathF.Max(-25f, MathF.Min(25f, newPosition.X)),
                newPosition.Y,
                MathF.Max(-25f, MathF.Min(25f, newPosition.Z))
            );

            
            if (!gameState.IsPositionBlocked(newPosition))
            {
                Position = newPosition;
            }
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

        public void Update(float deltaTime, Vector3D<float> playerPosition, GameState gameState)
        {
            if (!IsAlive) return;

            moveTimer += deltaTime;

            
            if (moveTimer >= 2f || moveDirection == Vector2D<float>.Zero)
            {
                moveTimer = 0f;
                
                
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

                
                Rotation = MathF.Atan2(moveDirection.X, moveDirection.Y);
            }

            
            var newPosition = Position + new Vector3D<float>(moveDirection.X * MoveSpeed * deltaTime, 0f, moveDirection.Y * MoveSpeed * deltaTime);

            
            newPosition = new Vector3D<float>(
                MathF.Max(-30f, MathF.Min(30f, newPosition.X)),
                newPosition.Y,
                MathF.Max(-30f, MathF.Min(30f, newPosition.Z))
            );

            
            if (!gameState.IsPositionBlocked(newPosition))
            {
                Position = newPosition;
            }
            else
            {
                
                var random = new Random();
                moveDirection = new Vector2D<float>(
                    random.NextSingle() * 2f - 1f,
                    random.NextSingle() * 2f - 1f
                );
                moveDirection = Vector2D.Normalize(moveDirection);
                moveTimer = 0f; 
            }
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