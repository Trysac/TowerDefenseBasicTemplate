using System;
using UnityEngine;

namespace SaucerSiege.Game
{
    /// <summary>The Kenney tiles and props of one ground theme.</summary>
    [Serializable]
    public sealed class ThemeTiles
    {
        public GameObject Ground;
        public GameObject Straight;
        public GameObject Corner;
        public GameObject Spawn;
        public GameObject End;
        public GameObject[] Trees;
        public GameObject[] Rocks;
        public GameObject[] Crystals;

        // Which sides of each road tile are open when it is not rotated, as Side bits.
        // ProjectBuilder measures them from where the road material sits in each mesh.
        public int StraightOpen;
        public int CornerOpen;
        public int SpawnOpen;
        public int EndOpen;
    }

    /// <summary>The Kenney pieces a tower kind is stacked from.</summary>
    [Serializable]
    public sealed class TowerParts
    {
        public GameObject Bottom;
        public GameObject Middle;
        public GameObject Top;
        public GameObject Weapon;
    }

    /// <summary>
    /// Every asset the game uses, gathered by the editor's ProjectBuilder so the scene only needs
    /// one reference. Models are Kenney's Tower Defense Kit (CC0); audio is synthesized.
    /// </summary>
    [CreateAssetMenu(menuName = "Saucer Siege/Game Art")]
    public sealed class GameArt : ScriptableObject
    {
        [Header("Ground")]
        /// <summary>Width of a Kenney tile in model units (the board scales everything to 1 unit per tile).</summary>
        public float TileSize = 1f;
        /// <summary>Height of a tile's top surface in model units.</summary>
        public float TileTop = 0.2f;
        public ThemeTiles Meadow;
        public ThemeTiles Snow;
        public GameObject Castle;

        [Header("Towers (Ballista, Cannon, Frost, Mortar)")]
        public TowerParts[] Towers;

        [Header("Saucers (Scout, Raider, Tank, Mothership)")]
        public GameObject[] Saucers;

        [Header("Materials")]
        public Material Overlay;
        public Material ParticleAdditive;
        public Material ParticleAlpha;
        public Material Bolt;
        public Material Shell;
        public Material Boulder;
        public Material FrostOrb;
        public Material BarBack;
        public Material BarFill;

        [Header("Meshes")]
        public Mesh CubeMesh;
        public Mesh SphereMesh;
        public Mesh QuadMesh;

        [Header("Music")]
        public AudioClip TitleMusic;
        public AudioClip BattleMusic;

        [Header("Sound effects")]
        public AudioClip Click;
        public AudioClip Denied;
        public AudioClip Build;
        public AudioClip Upgrade;
        public AudioClip Sell;
        public AudioClip BallistaShot;
        public AudioClip CannonShot;
        public AudioClip FrostShot;
        public AudioClip MortarShot;
        public AudioClip Explosion;
        public AudioClip Hit;
        public AudioClip SaucerDown;
        public AudioClip MothershipDown;
        public AudioClip Leak;
        public AudioClip WaveStart;
        public AudioClip WaveClear;
        public AudioClip Victory;
        public AudioClip Defeat;

        public ThemeTiles For(SaucerSiege.Core.Theme theme) => theme == SaucerSiege.Core.Theme.Snow ? Snow : Meadow;
    }
}
