using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuterra.World.Biomes;
using Nuterra.World.Chunks;
using Nuterra.World.Scenery;
using TerraTechETCUtil;
using UnityEngine;
using static WaterMod.SurfacePool;


namespace WaterMod
{
    internal class OceanDebuggerGUI : MonoBehaviour
    {
        static OceanDebuggerGUI helpGUI = null;
        private Rect Window = new Rect(0, 0, 280, 145);

        public static float value = 100f;
        public static float value2 = 50f;
        public static float value3 => ManOceanGenerator.seaWeights;

        public static void Init()
        {
            if (helpGUI == null)
                helpGUI = new GameObject().AddComponent<OceanDebuggerGUI>();
        }
        private void OnGUI()
        {
            try
            {
                Window = GUI.Window(2958715, Window, GUIWindow, "Settings");
            }
            catch { }
        }
        private void GUIWindow(int ID)
        {
            GUILayout.Label("Value: " + value.ToString("F"));
            value = GUILayout.HorizontalSlider(value, 100f, 1000000f);
            GUILayout.Label("seaWeights: " + value3.ToString("F"));
            ManOceanGenerator.seaWeights = GUILayout.HorizontalSlider(ManOceanGenerator.seaWeights, 1f, 15000f);
            if (GUILayout.Button("RESET TERRAIN"))
            {
                ManOceanGenerator.ResetBiomeTrotter();
                ManWorldTileExt.HostOnly_ReloadENTIREScene(true);
            }

            GUI.DragWindow();
        }
    }
    internal class ManOceanGenerator : ManWorldGeneratorExt
    {
        public const int numExpectedBiomes = 19;

        /// <summary>
        /// Currently messes with TOO much
        /// </summary>
        public static bool ApplySeaToALL = false;
        public static AnimationCurve SeaDistanceWeighting => AnimationCurve.Linear(0f, 1f, OceanDebuggerGUI.value, 0f); //new AnimationCurve();

        public const float HeightOffsetMulti = 1f / TerrainOperations.TileHeightDefault;


        //public static float seaWeights = 0.195f;//0.25f;
        //public static float seaWeights = 15000f;
        public static float seaWeights = 0.4f;
        private static float seaWeightsBeaches => 0.6f * seaWeights;
        /// <summary> OFFSET FROM NORMAL TERRAIN GEN </summary>
        private const float SeaBeachHeightScaled = HeightOffsetMulti * SeaBeachHeight;//-0.2f;
        public const float SeaBeachHeight = -22.5f;//-0.2f;
        private static float seaWeightsBeachesSubmerged => 0.8f * seaWeights;
        /// <summary> OFFSET FROM NORMAL TERRAIN GEN </summary>
        private const float SeaBeachSubHeightScaled = HeightOffsetMulti * SeaBeachSubHeight;
        public const float SeaBeachSubHeight = -32.5f;//-0.2f;
        private static float seaWeightsShallows => 0.3f * seaWeights;
        /// <summary> OFFSET FROM NORMAL TERRAIN GEN </summary>
        private const float SeaShallowsHeightScaled = HeightOffsetMulti * SeaShallowsHeight;
        public const float SeaShallowsHeight = -60f;
        private static float seaWeightsFauna => 0.4f * seaWeights;
        /// <summary> OFFSET FROM NORMAL TERRAIN GEN </summary>
        private const float SeaFaunaHeightScaled = HeightOffsetMulti * SeaFaunaHeight;
        public const float SeaFaunaHeight = -110f;
        private static float seaWeightsDeep => 0.13f * seaWeights;
        /// <summary> OFFSET FROM NORMAL TERRAIN GEN </summary>
        private const float SeaDeepHeightScaled = HeightOffsetMulti * SeaDeepHeight;
        public const float SeaDeepHeight = 150f;
        private static float seaWeightsFloor => 0.1f * seaWeights;
        /// <summary> OFFSET FROM NORMAL TERRAIN GEN </summary>
        private const float SeaFloorHeightScaled = HeightOffsetMulti * SeaFloorHeight;
        public const float SeaFloorHeight = -175f;


        public static bool oceanBiomesAllReady = false;
        public static bool applied = false;
        public static Dictionary<string, Biome> SeabiomesByName = null;
        private static Dictionary<Biome, float> SeabiomeToWeight = null;
        private static BiomeGroup seaBiomeGroup = null;

        /*
            Biome #0 - BasicGrasslandBiome_ScaledTrees
            Biome #1 - CopseOfTreesSubBiome
            Biome #2 - RockyRidgeBiome
            Biome #3 - WoodlandValleyBiome
            Biome #4 - DesertBiome
            Biome #5 - MogulsBiome
            Biome #6 - SmallDunesBiome
            Biome #7 - LowMesasBiome
            Biome #8 - LargeDunesBiome
            Biome #9 - FlatsBiome
            Biome #10 - MountainsBiome
            Biome #11 - TerracedHillsBiome
            Biome #12 - PeaksBiome
            Biome #13 - CanyonsBiome
            Biome #14 - EaglesNestBiome
            Biome #15 - GorgesBiome
            Biome #16 - StepSlopesBiome
            Biome #17 - PillarsBiome
            Biome #18 - IceBiome
            Biome #19 - LargeCraters_Biome
            Biome #20 - MidCraters_Biome
            Biome #21 - SmallCraters_Biome
        */
        public const string ParadoxialBeachesBiomeName = "ParadoxialBeachesBiome";
        public const string SandShoalsBiomeName = "SandShoalsBiome";
        public const string SoulShoalsBiomeName = "SoulShoalsBiome";
        public const string MossyCreekBiomeName = "MossyCreekBiome";
        public const string JaggedSeasBiomeName = "JaggedSeasBiome";
        public const string WindingMoundsBiomeName = "WindingMoundsBiome";
        public const string ArcticFractureBiomeName = "ArcticFractureBiome";
        public const string PillarsShoreBiomeName = "PillarsShoreBiome";
        public const string ImpactSeaBiomeName = "ImpactSeaBiome";
        public static string[] AllSeaBiomes = new string[]
            {
                ParadoxialBeachesBiomeName,
                SandShoalsBiomeName,
                SoulShoalsBiomeName,
                MossyCreekBiomeName,
                JaggedSeasBiomeName,
                WindingMoundsBiomeName,
                ArcticFractureBiomeName,
                PillarsShoreBiomeName,
                ImpactSeaBiomeName,
            };
        public static IEnumerable<KeyValuePair<Biome, float>> ApplyOceanicBiomes(List<Biome> biomes)
        {
            if (biomes == null)
                throw new ArgumentNullException(nameof(biomes));
            if (biomes.Count < numExpectedBiomes)
            {
                DebugWater.Log("We expected " + numExpectedBiomes + " biomes, but only found " + biomes.Count + "? Let's see what exists...");
                foreach (var biome in biomes)
                    DebugWater.Log("- " + (biome.name.NullOrEmpty() ? "<NULL>" : biome.name));
                throw new IndexOutOfRangeException(nameof(biomes));
            }

            // The way biomeGroups work is that the lower priority biomes form in the middle
            // Desert sea
            Biome biomer = CopyBiome(biomes[8], ParadoxialBeachesBiomeName);
            SinkBiomeTEMP(biomer, SeaBeachHeightScaled, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsBeaches);
            biomer = CopyBiome(biomes[5], SandShoalsBiomeName);
            SinkBiomeTEMP(biomer, SeaBeachSubHeightScaled, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsBeachesSubmerged);

            // GrasslandsSea
            biomer = CopyBiome(biomes[7], SoulShoalsBiomeName);
            SinkBiomeTEMP(biomer, SeaFaunaHeightScaled, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsFauna);
            biomer = CopyBiome(biomes[2], MossyCreekBiomeName);
            SinkBiomeTEMP(biomer, SeaFaunaHeightScaled, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsFauna);

            // Mountains sea
            biomer = CopyBiome(biomes[12], JaggedSeasBiomeName);
            SinkBiomeTEMP(biomer, SeaFloorHeightScaled, false);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsFloor);
            biomer = CopyBiome(biomes[15], WindingMoundsBiomeName);
            SinkBiomeTEMP(biomer, SeaDeepHeightScaled, true, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsDeep);

            // Ice sea
            biomer = CopyBiome(biomes[18], ArcticFractureBiomeName);
            SinkBiomeTEMP(biomer, SeaFaunaHeightScaled, false, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsShallows * 0.07f);

            // Pillars sea
            biomer = CopyBiome(biomes[17], PillarsShoreBiomeName);
            SinkBiomeTEMP(biomer, SeaBeachHeightScaled, false);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsBeaches * 0.03f);

            // Wasteland sea
            biomer = CopyBiome(biomes[19], ImpactSeaBiomeName);
            SinkBiomeTEMP(biomer, SeaDeepHeightScaled, true, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsDeep * 0.3f);
        }


        private static bool seaBiomesExist = false;
        private static bool rebootManOceanGenerator = false;
        public static Dictionary<string, string> SceneryWaterVariants = null;
        public static Dictionary<SceneryTypes, string> SceneryToReplaceInWater = new Dictionary<SceneryTypes, string>()
        {
            { SceneryTypes.MountainTree, "CoralCoarse" },
            { SceneryTypes.DeadTree, "DeadCoral" },
            { SceneryTypes.ShroomTree, "MushCoral" },
            { SceneryTypes.ConeTree, "Seaweed" },
            { SceneryTypes.ChristmasTree, "CoralCoarse" },
            { SceneryTypes.DesertTree, "TubeLauncher" },
        };
        public static Dictionary<string, string[]> BiomesToAppend = new Dictionary<string, string[]>()
        {
            //{ "GrasslandGroupStart", new string[] { MossyCreekBiomeName} }, // Starter biome shouldn't have water
            { "GrasslandGroupBasic", new string[] { MossyCreekBiomeName } },
            { "GrasslandGroupAdvanced", new string[] { MossyCreekBiomeName, SoulShoalsBiomeName } },
            { "DesertGroupBasic", new string[] { ParadoxialBeachesBiomeName } },
            { "DesertGroupAdvanced", new string[] { ParadoxialBeachesBiomeName, SandShoalsBiomeName } },
            //{ "FlatsGroupBasic", new string[] {  } },
            { "MountainsGroupBasic", new string[] { WindingMoundsBiomeName } },
            { "MountainsGroupAdvanced", new string[] { WindingMoundsBiomeName, JaggedSeasBiomeName } },
            { "PillarsGroup", new string[] { PillarsShoreBiomeName } },
            { "IceGroupAdvanced", new string[] { ArcticFractureBiomeName } },
            { "Biome7Group", new string[] { ImpactSeaBiomeName } },
        };

        private static FieldInfo distWeight = typeof(BiomeGroup).GetField("m_WeightingByDistance", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void ResetBiomeTrotter()
        {
            rebootManOceanGenerator = true;
        }
        public static void CleanupMess()
        {
            foreach (var item in SeabiomeToWeight)
            {
                if (LowerTerrainHeightClamped.ContainsKey(item.Key.HeightMapGenerator))
                {
                    DebugWater.Log("Removing clamp from sea biome(2) - Unexpected");
                    LowerTerrainHeightClamped.Remove(item.Key.HeightMapGenerator);
                }
            }
        }



        public static void HookToBiomes()
        {
            ManModChunks.ScriptCreateEvent.Subscribe(OnChunksCreationStarted);
            ManModScenery.ScriptCreateEvent.Subscribe(OnSceneryCreationStarted);
            ManModBiomes.ScriptCreateBiomeEvent.Subscribe(OnBiomesCreationStarted);
        }
        public static void UnHookFromBiomes()
        {
            ManModBiomes.ScriptCreateBiomeEvent.Unsubscribe(OnBiomesCreationStarted);
            ManModScenery.ScriptCreateEvent.Unsubscribe(OnSceneryCreationStarted);
            ManModChunks.ScriptCreateEvent.Unsubscribe(OnChunksCreationStarted);
        }

        public static void OnChunksCreationStarted(ManModChunks.CreateRequest reload)
        {
            reload.RequestToCreate(QPatch.ModNameAssetBundle, 
                new CustomChunk("Coralsite")
                {
                    Name = "Coralsite",
                    Description = "dead coral",
                    PrefabName = SpawnHelper.GetResourceChunkPrefabs().First().name,
                    Cost = 26,
                    Rarity = ChunkRarity.Uncommon,
                    Mass = 50,
                    StaticFriction = 0,
                    DynamicFriction = 0,
                    Restitution = 1f,
                    Health = 100,
                    DamageableType = ManDamage.DamageableType.Standard,
                    IsRefined = false,
                    IsFuel = false,
                    ComponentTier = ComponentTier.Null,
                    FuelTime = 0,
                    FuelEnergy = 350,
                    MeshName = "Coralsite.obj",
                    TextureName = "Coralsite.png",
                    JSONData = new Dictionary<string, object>(),
                });
        }
        public static void OnSceneryCreationStarted(ManModScenery.CreateRequest reload)
        {
            reload.RequestToCreate(QPatch.ModNameAssetBundle,
                new CustomScenery("TubeCoral")
                {
                    Name = "Tube Coral",
                    Description = "Mysterious coral creature",
                    PrefabName = SpawnHelper.SceneryTypeToFirstSceneryName[SceneryTypes.ShroomTree],
                    GroundRadius = 1.5f,
                    MaxHeightOffset = 0f,
                    MinHeightOffset = 0.1f,
                    Health = 350,
                    DamageableType = ManDamage.DamageableType.Wood,
                    Hostile = false,
                    Roving = false,
                    MeshName = "TubeCoral.obj",
                    TextureName = "TubeCoral.png",
                    JSONData = new Dictionary<string, object>(),
                    AttackedChunks = 0,
                });
        }
        public static void OnBiomesCreationStarted(ManModBiomes.CreateRequestBiome reload)
        {
            var oInst = KickStartWaterMod.oInst;
            var MC = oInst.GetModContainer();
            foreach (var item in AllSeaBiomes)
            {
                string rawData = ResourcesHelper.FetchTextData(MC, string.Empty);
                if (rawData != null)
                    reload.CreateFromJSON(QPatch.ModNameAssetBundle, JObject.Parse(rawData), "WaterBiome");
            }
            //ManModBiomes.RegisterToLoad();
        }


        private static void AddWaterScenery()
        {
            if (SceneryWaterVariants == null)
            {
                SceneryWaterVariants = new Dictionary<string, string>();
                foreach (var item in SpawnHelper.IterateAllScenery())
                {
                    var itemType = item.Value?.GetComponent<Visible>()?.m_ItemType;
                    if (itemType != null && item.Key != null && 
                        SceneryToReplaceInWater.TryGetValue((SceneryTypes)itemType.ItemType, out string vak))
                        SceneryWaterVariants.Add(item.Key, vak);
                }
            }
        }



        private static void RegenerateSeaBiomesToWeight(List<Biome> biomes)
        {
            SeabiomesByName = new Dictionary<string, Biome>();
            SeabiomeToWeight = new Dictionary<Biome, float>();
            for (int step = 0; step < biomes.Count; step++)
            {
                var item = biomes[step];
                if (item.name == "MuddyPondBiome")
                {
                    DebugWater.LogGen("Biomes ready!");
                    break;
                }
            }
            DebugWater.LogGen("Getting current biomes...");
            for (int step = 0; step < biomes.Count; step++)
            {
                var item = biomes[step];
                DebugWater.LogGen("Biome #" + step + " - " + item.name);
            }
            DebugWater.LogGen("Generating new biomes...");
            foreach (var item in ApplyOceanicBiomes(biomes))
            {
                SeabiomesByName.Add(item.Key.name, item.Key);
                SeabiomeToWeight.Add(item.Key, item.Value);
                new WikiPageBiome(item.Key);
            }
        }

        private static void RegenerateSeaBiomesGroup(List<Biome> biomes, List<BiomeGroup> biomesGrouped)
        {
            Biome[] biomeShoehorn = new Biome[SeabiomeToWeight.Count];
            float[] biomeWeights = new float[SeabiomeToWeight.Count];
            for (int i = 0; i < SeabiomeToWeight.Count; i++)
            {
                var stepC = SeabiomeToWeight.ElementAt(i);
                biomeShoehorn[i] = stepC.Key;
                biomeWeights[i] = stepC.Value;
            }
            seaBiomeGroup = CopyBiomeGroup(biomesGrouped.First(),
            "SeaBiomeGroup", biomeShoehorn, biomeWeights);
            AnimationCurve AC = SeaDistanceWeighting;
            distWeight.SetValue(seaBiomeGroup, AC);

            foreach (var item in SeabiomeToWeight)
                biomes.Add(item.Key);
        }


        private static void AddSeaBiomesToAll(List<BiomeGroup> biomesGrouped)
        {
            foreach (var item in biomesGrouped)
            {
                if (item == null)
                {
                    DebugWater.LogGen("null?");
                    continue;
                }
                DebugWater.Log("Biome Group " + (item.name.NullOrEmpty() ? "<NULL>" : item.name));
                try
                {
                    Biome[] biomes2 = (Biome[])biomesInside.GetValue(item);
                    if (biomes2 == null)
                    {
                        DebugWater.Log("NULL Biomes?");
                        continue;
                    }

                    Array.Resize(ref biomes2, biomes2.Length + SeabiomeToWeight.Count);
                    for (int step2 = 0; step2 < SeabiomeToWeight.Count; step2++)
                    {
                        biomes2[biomes2.Length - (step2 + 1)] = SeabiomeToWeight.ElementAt(step2).Key;
                    }
                    biomesInside.SetValue(item, biomes2);
                    DebugWater.LogGen("Biomes added!");

                    // add the weights
                    float[] biomesWeightsCached = (float[])biomesWeights.GetValue(item);
                    if (biomesWeightsCached == null)
                    {
                        DebugWater.Log("NULL Biome Weights?");
                        continue;
                    }

                    Array.Resize(ref biomesWeightsCached, biomesWeightsCached.Length + SeabiomeToWeight.Count);
                    for (int step2 = 0; step2 < SeabiomeToWeight.Count; step2++)
                        biomesWeightsCached[biomes2.Length - (step2 + 1)] = SeabiomeToWeight.ElementAt(step2).Value;
                    biomesWeights.SetValue(item, biomesWeightsCached);
                    DebugWater.LogGen("Biome weights added for (" + (item.name.NullOrEmpty() ? "<NULL>" : item.name) + ")!");

                    // Sanity check
                    for (int step2 = 0; step2 < biomes2.Length; step2++)
                    {
                        DebugWater.LogGen("Biome " + (biomes2[step2].name.NullOrEmpty() ? "<NULL>" : biomes2[step2].name) +
                            " - " + biomesWeightsCached[step2].ToString());
                    }
                }
                catch (Exception e) { DebugWater.FatalError("error in " + nameof(AddSeaBiomesToAll) + " " + e); }
            }
        }
        private static void AddSeaBiomeWeightsToAll(List<BiomeGroup> biomesGrouped)
        {
            try
            {
                foreach (var item in biomesGrouped)
                {
                    Biome[] biomes2 = (Biome[])biomesInside.GetValue(item);
                    float[] biomesWeightsCached = (float[])biomesWeights.GetValue(item);
                    if (biomesWeightsCached == null)
                    {
                        DebugWater.Log("NULL Biome Weights?");
                        continue;
                    }

                    for (int step2 = 0; step2 < SeabiomeToWeight.Count; step2++)
                        biomesWeightsCached[biomes2.Length - (step2 + 1)] = SeabiomeToWeight.ElementAt(step2).Value;
                    biomesWeights.SetValue(item, biomesWeightsCached);
                    DebugWater.LogGen("Biome weights recalibrated for (" + (item.name.NullOrEmpty() ? "<NULL>" : item.name) + ")!");
                }
            }
            catch (Exception e) { DebugWater.FatalError("error in " + nameof(AddSeaBiomeWeightsToAll) + " " + e); }
        }

        private static void AddOnlySeaBiomes(List<BiomeGroup> biomesGrouped)
        {
            foreach (var item in biomesGrouped)
            {
                if (item == null)
                {
                    DebugWater.LogGen("null?");
                    continue;
                }
                DebugWater.Log("Biome Group " + (item.name.NullOrEmpty() ? "<NULL>" : item.name));
                try
                {
                    if (BiomesToAppend.TryGetValue(item.name, out var seaBiomeNames))
                    {
                        Biome[] biomes2 = (Biome[])biomesInside.GetValue(item);
                        if (biomes2 == null)
                        {
                            DebugWater.Log("NULL Biomes?");
                            continue;
                        }

                        // add the weights
                        float[] biomesWeightsCached = (float[])biomesWeights.GetValue(item);
                        if (biomesWeightsCached == null)
                        {
                            DebugWater.Log("NULL Biome Weights?");
                            continue;
                        }

                        // Sanity check
                        for (int step2 = 0; step2 < biomes2.Length; step2++)
                        {
                            DebugWater.LogGen("- Biome " + (biomes2[step2].name.NullOrEmpty() ? "<NULL>" : biomes2[step2].name) +
                                " - " + biomesWeightsCached[step2].ToString());
                        }

                        Array.Resize(ref biomes2, biomes2.Length + seaBiomeNames.Length);
                        try
                        {
                            for (int i = 0; i < seaBiomeNames.Length; i++)
                                biomes2[biomes2.Length - (1 + i)] = SeabiomesByName[seaBiomeNames[i]];
                        }
                        catch (Exception e)
                        {
                            for (int i = 0; i < seaBiomeNames.Length; i++)
                            {
                                if (!SeabiomesByName.ContainsKey(seaBiomeNames[i]))
                                    throw new Exception("Failed to find biome of name " + seaBiomeNames[i] + " in SeabiomesByName", e);
                            }
                        }
                        biomesInside.SetValue(item, biomes2);
                        DebugWater.LogGen("Biomes added!");

                        Array.Resize(ref biomesWeightsCached, biomesWeightsCached.Length + seaBiomeNames.Length);
                        try
                        {
                            for (int i = 0; i < seaBiomeNames.Length; i++)
                                biomesWeightsCached[biomes2.Length - (1 + i)] = SeabiomeToWeight[SeabiomesByName[seaBiomeNames[i]]];
                        }
                        catch (Exception e)
                        {
                            for (int i = 0; i < seaBiomeNames.Length; i++)
                            {
                                if (!SeabiomeToWeight.ContainsKey(SeabiomesByName[seaBiomeNames[i]]))
                                    throw new Exception("Failed to find biome of name " + seaBiomeNames[i] + " in SeabiomeToWeight", e);
                            }
                        }
                        biomesWeights.SetValue(item, biomesWeightsCached);
                        DebugWater.LogGen("Biome weights added for (" + (item.name.NullOrEmpty() ? "<NULL>" : item.name) + ")!");
                    }
                }
                catch (Exception e) { DebugWater.FatalError("error in " + nameof(AddOnlySeaBiomes) + " " + e); }
            }
        }
        private static void AddOnlySeaBiomeWeights(List<BiomeGroup> biomesGrouped)
        {
            foreach (var item in biomesGrouped)
            {
                if (item == null)
                {
                    DebugWater.LogGen("null?");
                    continue;
                }
                DebugWater.Log("Biome Group " + (item.name.NullOrEmpty() ? "<NULL>" : item.name));
                try
                {
                    if (BiomesToAppend.TryGetValue(item.name, out var seaBiomeNames))
                    {
                        Biome[] biomes2 = (Biome[])biomesInside.GetValue(item);
                        if (biomes2 == null)
                        {
                            DebugWater.Log("NULL Biomes?");
                            continue;
                        }

                        float[] biomesWeightsCached = (float[])biomesWeights.GetValue(item);
                        if (biomesWeightsCached == null)
                        {
                            DebugWater.Log("NULL Biome Weights?");
                            continue;
                        }

                        // Sanity check
                        for (int step2 = 0; step2 < biomes2.Length; step2++)
                        {
                            DebugWater.Log("- Biome " + (biomes2[step2].name.NullOrEmpty() ? "<NULL>" : biomes2[step2].name) +
                                " - " + biomesWeightsCached[step2].ToString());
                        }

                        for (int i = 0; i < seaBiomeNames.Length; i++)
                        {
                            biomes2[biomes2.Length - (1 + i)] = SeabiomesByName[seaBiomeNames[i]];
                        }
                        biomesInside.SetValue(item, biomes2);
                        DebugWater.LogGen("Biomes added!");

                        for (int i = 0; i < seaBiomeNames.Length; i++)
                            biomesWeightsCached[biomes2.Length - (1 + i)] = SeabiomeToWeight[SeabiomesByName[seaBiomeNames[i]]];
                        biomesWeights.SetValue(item, biomesWeightsCached);
                        DebugWater.LogGen("Biome weights recalibrated for (" + (item.name.NullOrEmpty() ? "<NULL>" : item.name) + ")!");
                    }
                }
                catch (Exception e) { DebugWater.FatalError("error in " + nameof(AddOnlySeaBiomeWeights) + " " + e); }
            }
        }


        private static void DisableSeaBiomeGroupAssignments(List<BiomeGroup> biomesGrouped)
        {
            foreach (var item in biomesGrouped)
            {
                if (item == seaBiomeGroup)
                    return; // DON'T TOUCH THE SEA BIOME!!!!
                Biome[] biomes2 = (Biome[])biomesInside.GetValue(item);
                float[] biomesWeightsCached = (float[])biomesWeights.GetValue(item);
                if (biomesWeightsCached == null)
                {
                    DebugWater.Log("NULL Biome Weights?");
                    continue;
                }
                for (int step2 = 0; step2 < biomes2.Length; step2++)
                {
                    if (SeabiomeToWeight.ContainsKey(biomes2[step2]))
                        biomesWeightsCached[step2] = 0;
                }
                biomesWeights.SetValue(item, biomesWeightsCached);
            }
        }





        private static bool PRIMARY_SanityCheckAndGetBiomes(BiomeMap biomesMain,
            out BiomeGroup[] ogBiomeGroups, ref List<Biome> biomesCopy)
        {
            ogBiomeGroups = null;
            if (biomesMain == null)
            {
                DebugWater.Log("Could not change biomes: Biomes is not loaded...");
                return false;
            }
            if (ManWorld.inst?.TileManager == null)
            {
                DebugWater.Log("Could not change biomes: TileManager is not loaded...");
                return false;
            }
            ogBiomeGroups = biomesBatched2.GetValue(biomesMain) as BiomeGroup[];
            if (ogBiomeGroups == null || ogBiomeGroups.Length == 0)
            {
                DebugWater.Log("Could not change biomes: BiomeGroups is not loaded...");
                return false;
            }
            biomesCopy.Clear();
            var biomesDataGet = biomesData.GetValue(biomesMain);
            if (biomesDataGet == null || biomesAll.GetValue(biomesDataGet) == null)
            {   // Get from the existing default pool
                foreach (BiomeGroup biomeGroup in ogBiomeGroups)
                {
                    for (int step = 0; step < biomeGroup.Biomes.Length; step++)
                    {
                        Biome biom = biomeGroup.Biomes[step];
                        if (!biomesCopy.Contains(biom))
                            biomesCopy.Add(biom);
                    }
                }
            }
            else
                biomesCopy.AddRange((Biome[])biomesAll.GetValue(biomesDataGet)); // get from BiomeGroupDatabase
            return true;
        }
        private static void SetBiomesAndGroups(BiomeMap biomesMain,
           List<BiomeGroup> biomeGroups, List<Biome> allBiomes)
        {
            recursionStopper = true;
            try
            {
                ManWorld.inst.TileManager.PauseGenerationOneFrame();
                biomesMain.InvalidateBiomeDB();
                var sharedBGArray = biomeGroups.ToArray();
                var biomesDataGet = biomesData.GetValue(biomesMain);
                if (biomesDataGet != null)
                {
                    biomesAll.SetValue(biomesDataGet, new List<Biome>(allBiomes));
                    biomesBatched.SetValue(biomesDataGet, sharedBGArray);
                }
                biomesBatched2.SetValue(biomesMain, sharedBGArray);
                biomesMain.LookupBiome(0);
                ManWorld.inst.Reset(ManWorld.inst.CurrentBiomeMap);
                ManWorldTileExt.RushTileLoading();
            }
            finally
            {
                recursionStopper = false;
            }
        }

        private static List<Biome> curList = new List<Biome>();
        private static bool recursionStopper = false;



        public static void InitiateAndOrEnableOceanicBiomes(BiomeMap biomesMain)
        {
            //DebugWater.Log("InitiateAndOrEnableOceanicBiomes");
            if (recursionStopper)
                return;
            //OceanDebuggerGUI.Init();

            //Debug_TTExt.ShouldLogBiomeGen = true;
            int errorCode = 0;
            try
            {
                if (biomesData == null)
                    throw new NullReferenceException(nameof(biomesData));
                if (biomesAll == null)
                    throw new NullReferenceException(nameof(biomesAll));
                if (biomesInside == null)
                    throw new NullReferenceException(nameof(biomesInside));
                if (biomesWeights == null)
                    throw new NullReferenceException(nameof(biomesWeights));
                if (biomesBatched == null)
                    throw new NullReferenceException(nameof(biomesBatched));
                if (biomesBatched2 == null)
                    throw new NullReferenceException(nameof(biomesBatched2));

                if (rebootManOceanGenerator)
                {
                    rebootManOceanGenerator = false;
                    DebugWater.Log("REBOOT OCEAN");
                    seaBiomesExist = false;
                    DisableOceanicBiomes(biomesMain);
                    SeabiomeToWeight = null;
                }
                else if (oceanBiomesAllReady)
                    return;

                if (applied)
                    return;
                applied = true;

                DebugWater.Log("Ocean man why don't you take me by the hand~");
                if (!PRIMARY_SanityCheckAndGetBiomes(biomesMain, out var groupB, ref curList))
                    return;

                InsureInit();

                OnClampTerrain.Subscribe(CleanupMess);
                oceanBiomesAllReady = true;

                //DebugWater.Log("Biomes: " + biomesMain.GetNumBiomes());
                List<BiomeGroup> biomesGrouped = groupB.ToList();
                var biomesDataGet = biomesData.GetValue(biomesMain);
                errorCode++;
                if (curList.Count < numExpectedBiomes)
                {
                    DebugWater.Log("We are not in the main game. We cannot apply any changes made by " + nameof(ManOceanGenerator));
                    return;
                }

                if (!ManPointer.inst.IsInteractionBlocked)
                    UIHelpersExt.BigF5broningBannerSP("Rebuilding planet...", false);

                AddWaterScenery();

                if (!seaBiomesExist)
                {
                    errorCode++;
                    if (SeabiomeToWeight == null)
                        RegenerateSeaBiomesToWeight(curList);

                    /*
                    //CHECK THIS
                    //if (biomesDataGet != null)
                    //    biomesAll.SetValue(biomesDataGet, biomes.ToArray());
                    //errorCode++;
                    //biomesAll2.SetValue(biomesMain, biomes.ToArray());
                    */

                    if (seaBiomeGroup == null)
                        RegenerateSeaBiomesGroup(curList, biomesGrouped);

                    errorCode = 15000;
                    biomesGrouped.Add(seaBiomeGroup);

                    errorCode++;
                    if (ApplySeaToALL)
                        AddSeaBiomesToAll(biomesGrouped);
                    else
                        AddOnlySeaBiomes(biomesGrouped);

                    errorCode++;
                    DebugWater.Log("Ocean Biomes setup!");
                    seaBiomesExist = true;
                }
                else
                {
                    if (ApplySeaToALL)
                        AddSeaBiomeWeightsToAll(biomesGrouped);
                    else
                        AddOnlySeaBiomeWeights(biomesGrouped);

                    biomesGrouped.Add(seaBiomeGroup);
                    errorCode++;
                    DebugWater.Log("Ocean Biomes reloaded");
                }

                SetBiomesAndGroups(biomesMain, biomesGrouped, curList);
            }
            catch (Exception e)
            {
                throw new Exception("Failed at " + errorCode, e);
            }
        }

        public static void DisableOceanicBiomes(BiomeMap biomesMain)
        {
            //DebugWater.Log("DisableOceanicBiomes");
            if (!applied)
                return;
            int errorCode = 0;
            applied = false;
            try
            {
                if (!PRIMARY_SanityCheckAndGetBiomes(biomesMain, out var groupB, ref curList))
                    return;

                OnClampTerrain.Unsubscribe(CleanupMess);
                oceanBiomesAllReady = false;

                //DebugWater.Log("Biomes: " + biomesMain.GetNumBiomes());
                List<BiomeGroup> biomesGrouped = groupB.ToList();
                if (curList.Count < numExpectedBiomes)
                {
                    DebugWater.Log("We are not in the main game. We cannot apply any changes made by " + nameof(ManOceanGenerator));
                    return;
                }

                if (!ManPointer.inst.IsInteractionBlocked)
                    UIHelpersExt.BigF5broningBannerSP("Rebuilding planet...", false);

                biomesGrouped.Remove(seaBiomeGroup);
                DisableSeaBiomeGroupAssignments(biomesGrouped);

                errorCode++;
                SetBiomesAndGroups(biomesMain, biomesGrouped, curList);
                DebugWater.Log("Ocean Biomes removed");
            }
            catch (Exception e)
            {
                throw new Exception("Failed at " + errorCode, e);
            }
        }



        private static void SinkBiomeTEMP(Biome biome, float depthScaler, bool blockScenery, bool invertDelta = false)
        {
            if (blockScenery)
            {
                /*
                foreach (var item in biome.DetailLayers)
                {
                    BlockBiomeSceneryInWater(item.generator, true);
                }
                */
            }
            if (biome.HeightMapGenerator.m_UseLegacy)
            {
                biome.HeightMapGenerator.EditorInitFromLegacyParams();
                biome.HeightMapGenerator.m_UseLegacy = false;
            }

            MapGenerator.Layer[] Layers = (MapGenerator.Layer[])layers.GetValue(biome.HeightMapGenerator);
            if (Layers != null)
            {
                DebugWater.LogGen("Biome " + biome.name);
                allowTS.SetValue(biome, false);
                allowMarks.SetValue(biome, false);
                allowStunts.SetValue(biome, false);

                var ogLayerEnd = Layers[Layers.Length - 1];
                float totalWeight = 1;
                foreach (var item in Layers)
                {
                    totalWeight += item.weight;
                }
                if (invertDelta)
                {
                    Array.Resize(ref Layers, Layers.Length + 2);
                    var layerInv = CopyLayer(ogLayerEnd, -1);
                    layerInv.generator = GenDelegateNone;
                    layerInv.amplitude = -1;
                    layerInv.bias = 0;
                    layerInv.weight = 0.5f;
                    layerInv.operations = new MapGenerator.Operation[1]
                        {
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Modify, -1),
                        };
                    layerInv.applyOperation = MapGenerator.Operation.New(MapGenerator.Operation.Code.Mul, 0);

                    Layers[Layers.Length - 2] = layerInv;
                }
                else
                {
                    Array.Resize(ref Layers, Layers.Length + 1);
                }
                var layerEnd = CopyLayer(ogLayerEnd, -1);
                layerEnd.generator = GenDelegateNone;
                layerEnd.amplitude = 0;
                layerEnd.bias = depthScaler / (1 / totalWeight);
                if (invertDelta)
                    layerEnd.weight = 0.5f;
                else
                    layerEnd.weight = 1;
                layerEnd.operations = new MapGenerator.Operation[1]
                    {
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Modify,
                    depthScaler * TerrainOperations.tileScaleToMapGen),
                    };
                layerEnd.applyOperation = MapGenerator.Operation.New(MapGenerator.Operation.Code.Add, 0);

                Layers[Layers.Length - 1] = layerEnd;
                layers.SetValue(biome.HeightMapGenerator, Layers);

                /*
                foreach (var item in Layers)
                {
                    DebugWater.Log("Layer - " + item.applyOperation.code);
                    foreach (var item2 in item.operations)
                    {
                        DebugWater.Log("- " + item2.code + (item2.buffered ? 
                            (", bufferIndex " + item2.index) : (", val " + item2.param)));
                    }
                }
                */
            }
        }

    }
}
