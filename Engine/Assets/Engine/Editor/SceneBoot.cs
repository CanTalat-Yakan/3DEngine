﻿using System.Numerics;
using System;
using Engine.Components;
using Editor.Controller;
using Engine.ECS;
using Engine.Utilities;

namespace Engine.Editor
{
    internal class SceneBoot : Component
    {
        public Entity Camera;

        public CameraController CameraController;

        private Entity _subParent;
        private Entity _special;

        public SceneBoot() { ScriptSystem.Register(this); }

        public override void Awake()
        {
            Camera = SceneManager.Scene.EntitytManager.CreateEntity(null, "Camera");
            Camera.Tag = ETags.MainCamera;
            Camera.Transform.Position = new Vector3(3, 4, 5);
            Camera.Transform.EulerAngles = new Vector3(35, -150, 0);
            Camera.AddComponent(new Camera());
            Camera.AddComponent(new CameraController());

            SceneManager.Scene.EntitytManager.CreateSky();
        }

        public override void Start()
        {
            _special = SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.SPECIAL);
            _special.Transform.Scale *= 0.1f;
            _special.Transform.Position.Y += 0.5f;

            Entity parent = SceneManager.Scene.EntitytManager.CreateEntity(null, "Content");

            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(0, 0, 1);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(0, 0, -3);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(0, 2.5f, 0);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(0, -4, 0);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(2, 0, 0);
            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.SPHERE, parent).Transform.Position = new Vector3(-1, 1, 0);

            _subParent = SceneManager.Scene.EntitytManager.CreateEntity(null, "Cubes");
            _subParent.Parent = parent;

            SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.CUBE, _subParent);
        }

        public override void Update()
        {
            SceneManager.Scene.EntitytManager.Sky.Transform.Position = Camera.Transform.Position;

            if (Input.Instance.GetKey(Windows.System.VirtualKey.F, EInputState.DOWN))
                _special.Transform.Position += _special.Transform.Forward;
            if (Input.Instance.GetKey(Windows.System.VirtualKey.G, EInputState.DOWN))
                _special.Transform.Position += _special.Transform.Right;
            if (Input.Instance.GetKey(Windows.System.VirtualKey.V, EInputState.DOWN))
                Camera.Transform.Position += Camera.Transform.Right;

            if (Input.Instance.GetKey(Windows.System.VirtualKey.C, EInputState.DOWN))
            {
                OutputController.Log("Spawned 10 Cubes");

                for (int i = 0; i < 10; i++)
                {
                    var newCube = SceneManager.Scene.EntitytManager.CreatePrimitive(EPrimitiveTypes.CUBE, _subParent);
                    newCube.Transform.EulerAngles = new Vector3(new Random().Next(1, 360), new Random().Next(1, 360), new Random().Next(1, 360));
                    newCube.Transform.Scale = new Vector3(new Random().Next(1, 3), new Random().Next(1, 3), new Random().Next(1, 3));
                }
            }
        }
    }

}