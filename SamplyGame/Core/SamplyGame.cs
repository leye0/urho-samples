﻿using System.Diagnostics;
using System.Threading.Tasks;
using SamplyGame.Aircrafts.Enemies;
using Urho;
using Urho.Gui;
using Urho.Physics;
using Urho.Actions;
using Urho.Shapes;

namespace SamplyGame
{
	public class SamplyGame : Application
	{
		const string CoinstFormat = "{0} coins";

		int coins;
		Scene scene;
		Text coinsText;

		public Player Player { get; private set; }

        public Viewport ViewportLeft { get; private set; }
        public Viewport ViewportRight { get; private set; }


		[Preserve]
        public SamplyGame() : base(new ApplicationOptions(assetsFolder: "Data") { Width = 1024, Height = 576, Orientation = ApplicationOptions.OrientationType.Landscape}) { }

		[Preserve]
		public SamplyGame(ApplicationOptions opts) : base(opts) { }

		static SamplyGame()
		{
			UnhandledException += (s, e) =>
			{
				if (Debugger.IsAttached)
					Debugger.Break();
				e.Handled = true;
			};
		}

		protected override void Start()
		{
			base.Start();
			CreateScene();
			Input.SubscribeToKeyDown(e =>
			{
				if (e.Key == Key.Esc) Exit();
				if (e.Key == Key.C) AddCollisionDebugBox(scene, true);
				if (e.Key == Key.V) AddCollisionDebugBox(scene, false);
			});
		}

		static void AddCollisionDebugBox(Node rootNode, bool add)
		{
			var nodes = rootNode.GetChildrenWithComponent<CollisionShape>(true);
			foreach (var node in nodes)
			{
				node.GetChild("CollisionDebugBox", false)?.Remove();
				if (!add)
					continue;
				var subNode = node.CreateChild("CollisionDebugBox");
				var box = subNode.CreateComponent<Box>();
				subNode.Scale = node.GetComponent<CollisionShape>().WorldBoundingBox.Size;
				box.Color = new Color(Color.Red, 0.4f);
			}
		}

		async void CreateScene()
		{
			scene = new Scene();
			scene.CreateComponent<Octree>();

			var physics = scene.CreateComponent<PhysicsWorld>();
			physics.SetGravity(new Vector3(0, 0, 0));

			// Camera
            var cameraNodeLeft = scene.CreateChild();
			//cameraNode.Position = (new Vector3(0.0f, 0.0f, -10.0f)); // Point of view for our aircraft
            cameraNodeLeft.Position = (new Vector3(-0.2f, 0.0f, -8.0f));
			cameraNodeLeft.CreateComponent<Camera>();

            // Camera
            var cameraNodeRight = scene.CreateChild();
            //cameraNode.Position = (new Vector3(0.0f, 0.0f, -10.0f)); // Point of view for our aircraft
            cameraNodeRight.Position = (new Vector3(0.2f, 0.0f, -8.0f));
            cameraNodeRight.CreateComponent<Camera>();


            Renderer.NumViewports = 2;

            var graphics = Graphics;
            var rectLeft = new IntRect(0, 0, graphics.Width / 2, graphics.Height);
            var rectRight = new IntRect(graphics.Width / 2, 0, graphics.Width, graphics.Height);

			ViewportLeft = new Viewport(Context, scene, cameraNodeLeft.GetComponent<Camera>(), rectLeft, null);
            ViewportRight = new Viewport(Context, scene, cameraNodeRight.GetComponent<Camera>(), rectRight, null);
            ViewportRight.CullCamera = ViewportLeft.Camera;

			if (Platform != Platforms.Android && Platform != Platforms.iOS)
			{
				RenderPath effectRenderPath = ViewportLeft.RenderPath.Clone();
				var fxaaRp = ResourceCache.GetXmlFile(Assets.PostProcess.FXAA3);
				effectRenderPath.Append(fxaaRp);
				ViewportLeft.RenderPath = effectRenderPath;
			}

			Renderer.SetViewport(0, ViewportLeft);
            Renderer.SetViewport(1, ViewportRight);

			var zoneNode = scene.CreateChild();
			var zone = zoneNode.CreateComponent<Zone>();
			zone.SetBoundingBox(new BoundingBox(-600.0f, 600.0f));
			zone.AmbientColor = new Color(1f, 1f, 1f);
			
			// UI
			coinsText = new Text();
			coinsText.HorizontalAlignment = HorizontalAlignment.Right;
			coinsText.SetFont(ResourceCache.GetFont(Assets.Fonts.Font), Graphics.Width / 20);
			UI.Root.AddChild(coinsText);
			Input.SetMouseVisible(true, false);

			// Background
			var background = new Background();
			scene.AddComponent(background);
			background.Start();

			// Lights:
			var lightNode = scene.CreateChild();
			lightNode.Position = new Vector3(0, -5, -40);
			lightNode.AddComponent(new Light { Range = 120, Brightness = 0.8f });

			// Game logic cycle
			bool firstCycle = true;
			while (true)
			{
				var startMenu = scene.CreateComponent<StartMenu>();
				await startMenu.ShowStartMenu(!firstCycle); //wait for "start"
				startMenu.Remove();
				await StartGame();
				firstCycle = false;
			}
		}

		async Task StartGame()
		{
			UpdateCoins(0);
			Player = new Player();
			var aircraftNode = scene.CreateChild(nameof(Aircraft));
			aircraftNode.AddComponent(Player);
			var playersLife = Player.Play();
			var enemies = new Enemies(Player);
			scene.AddComponent(enemies);
			SpawnCoins();
			enemies.StartSpawning();
			await playersLife;
			enemies.KillAll();
			aircraftNode.Remove();
		}
		
		async void SpawnCoins()
		{
			var player = Player;
			while (Player.IsAlive && player == Player)
			{
				var coinNode = scene.CreateChild();
				coinNode.Position = new Vector3(RandomHelper.NextRandom(-2.5f, 2.5f), 5f, 0);
				var coin = new Apple();
				coinNode.AddComponent(coin);
				await coin.FireAsync(false);
				await scene.RunActionsAsync(new DelayTime(3f));
				coinNode.Remove();
			}
		}

		public void OnCoinCollected() => UpdateCoins(coins + 1);

		void UpdateCoins(int amount)
		{
			if (amount % 5 == 0 && amount > 0)
			{
				// give player a MassMachineGun each time he earns 5 coins
				Player.Node.AddComponent(new MassMachineGun());
			}
			coins = amount;
			coinsText.Value = string.Format(CoinstFormat, coins);
		}
	}
}
