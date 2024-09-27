using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using TerraTechETCUtil;


namespace WaterMod
{
    internal class HelperGUI : MonoBehaviour
    {
        static HelperGUI helpGUI = null;
        private Rect Window = new Rect(0, 0, 280, 145);

        public static float value = 1000000f;
        public static float value2 = 50f;
        public static float value3 = 1f;

        public static void Init()
        {
            if (helpGUI == null)
                helpGUI = new GameObject().AddComponent<HelperGUI>();
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
            GUILayout.Label("seaWeights: " + value2.ToString("F"));
            OceanFormer.seaWeights = GUILayout.HorizontalSlider(OceanFormer.seaWeights, 1f, 15000f);
            if (GUILayout.Button("RESET TERRAIN"))
            {
                OceanFormer.ResetBiomeTrotter();
                ManWorldTileExt.ReloadENTIREScene();
            }

            GUI.DragWindow();
        }
    }
    internal class OceanFormer : WorldTerraformer
    {
        public static bool ApplySeaToALL = true;
        public static AnimationCurve SeaWeighting => AnimationCurve.Linear(0f, 1f, HelperGUI.value, 0f); //new AnimationCurve();

        //public static float seaWeights = 0.195f;//0.25f;
        public static float seaWeights = 15000f;
        private static float seaWeightsBeaches => 0.6f * seaWeights;
        public const float SeaBeachHeight = -0.225f;//-0.2f;
        private static float seaWeightsBeachesSubmerged => 0.8f * seaWeights;
        public const float SeaBeachSubHeight = -0.325f;
        private static float seaWeightsShallows => 0.3f * seaWeights;
        public const float SeaShallowsHeight = -0.6f;
        private static float seaWeightsFauna => 0.4f * seaWeights;
        public const float SeaFaunaHeight = -1.1f;
        private static float seaWeightsDeep => 0.13f * seaWeights;
        public const float SeaDeepHeight = -1.5f;
        private static float seaWeightsFloor => 0.1f * seaWeights;
        public const float SeaFloorHeight = -1.75f;


        public static bool ready = false;
        public static bool applied = false;
        private static Dictionary<Biome, float> Seabiomes = null;
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
        public static IEnumerable<KeyValuePair<Biome, float>> ApplyOceanicBiomes(List<Biome> biomes)
        {
            // The way biomeGroups work is that the lower priority biomes form in the middle
            // Desert sea
            Biome biomer = CopyBiome(biomes[8], "ParadoxalBeachesBiome");
            SinkBiomeTEMP(biomer, seaWeightsBeaches, true);
            yield return new KeyValuePair<Biome, float>(biomer, SeaBeachHeight);
            biomer = CopyBiome(biomes[5], "SandShoalsBiome");
            SinkBiomeTEMP(biomer, seaWeightsBeachesSubmerged, true);
            yield return new KeyValuePair<Biome, float>(biomer, SeaBeachSubHeight);

            // GrasslandsSea
            biomer = CopyBiome(biomes[7], "SoulShoalsBiome");
            //biomer = CopyBiome(biomes[2], "MossyCreekBiome");
            SinkBiomeTEMP(biomer, SeaFaunaHeight, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsFauna);

            // Mountains sea
            biomer = CopyBiome(biomes[12], "JaggedSeasBiome");
            SinkBiomeTEMP(biomer, SeaFloorHeight, false);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsFloor);
            biomer = CopyBiome(biomes[15], "WindingMoundsBiome");
            SinkBiomeTEMP(biomer, SeaDeepHeight, true, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsDeep);

            // Ice sea
            biomer = CopyBiome(biomes[18], "ArcticFractureBiome");
            SinkBiomeTEMP(biomer, SeaFaunaHeight, false, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsShallows * 0.07f);

            // Pillars sea
            biomer = CopyBiome(biomes[17], "PillarsShoreBiome");
            SinkBiomeTEMP(biomer, SeaBeachHeight, false);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsBeaches * 0.03f);

            // Wasteland sea
            biomer = CopyBiome(biomes[19], "ImpactSeaBiome");
            SinkBiomeTEMP(biomer, SeaDeepHeight, true, true);
            yield return new KeyValuePair<Biome, float>(biomer, seaWeightsDeep * 0.3f);
        }
        public static KeyValuePair<Biome, float> TryGetPondBiome(string groupName, List<Biome> biomes)
        {
            Biome biomer;
            switch (groupName)
            {
                case "yes":
                    biomer = CopyBiome(biomes[2], "MossyCreekBiome");
                    SinkBiomeTEMP(biomer, SeaFaunaHeight, true);
                    return new KeyValuePair<Biome, float>(biomer, seaWeightsFauna);
                default:
                    break;
            }
            return default;
        }


        private static bool appended = false;
        private static bool Reboot = false;
        public static Dictionary<string, string> ObjectTypesWaterVariants = null;
        public static Dictionary<SceneryTypes, string> ObjectTypesToReplaceInWater = new Dictionary<SceneryTypes, string>()
        {
            { SceneryTypes.MountainTree, "CoralCoarse" },
            { SceneryTypes.DeadTree, "DeadCoral" },
            { SceneryTypes.ShroomTree, "MushCoral" },
            { SceneryTypes.ConeTree, "Seaweed" },
            { SceneryTypes.ChristmasTree, "CoralCoarse" },
            { SceneryTypes.DesertTree, "TubeLauncher" },
        };

        private static FieldInfo distWeight = typeof(BiomeGroup).GetField("m_WeightingByDistance", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void ResetBiomeTrotter()
        {
            Reboot = true;
        }
        public static void CleanupMess()
        {
            foreach (var item in Seabiomes)
            {
                if (LowerTerrainHeightClamped.ContainsKey(item.Key.HeightMapGenerator))
                {
                    DebugWater.Log("Removing clamp from sea biome(2) - Unexpected");
                    LowerTerrainHeightClamped.Remove(item.Key.HeightMapGenerator);
                }
            }
        }
        public static void AddOceanicBiomes(BiomeMap biomesMain)
        {
            if (applied)
                return;
            if (ObjectTypesWaterVariants == null)
            {
                try
                {
                    TAC_AI.KickStart.TerrainHeight = TerrainOperations.TileHeightRescaled;
                    TAC_AI.KickStart.TerrainHeightOffset = TerrainOperations.TileYOffsetRescaled;
                }
                catch (Exception)
                {
                    throw;
                }
                ObjectTypesWaterVariants = new Dictionary<string, string>();
                foreach (var item in SpawnHelper.IterateSceneryTypes())
                {
                    foreach (var item2 in item.Values)
                    {
                        foreach (var item3 in item2)
                        {
                            try
                            {
                                if (ObjectTypesToReplaceInWater.TryGetValue((SceneryTypes)item3.GetComponent<Visible>().ItemType, out string vak))
                                    ObjectTypesWaterVariants.Add(item3.name, vak);
                            }
                            catch { }
                        }
                    }
                }
            }
            InsureInit();
            applied = true;
            int errorCode = 0;
            try
            {
                if (Reboot)
                {
                    Reboot = false;
                    DebugWater.Log("REBOOT OCEAN");
                    appended = false;
                    RemoveOceanicBiomes(biomesMain);
                    Seabiomes = null;
                }
                else if (ready)
                    return;
                OnClampTerrain.Subscribe(CleanupMess);
                TerrainOperations.BeachingMode = true;

                DebugWater.Log("Ocean man why don't you take me by the hand~");
                if (biomesMain == null)
                {
                    DebugWater.Log("Could not change biomes: Biomes were not loaded...");
                    return;
                }
                if (ManWorld.inst?.TileManager == null)
                {
                    DebugWater.Log("Could not change biomes: TileManager was not loaded...");
                    return;
                }
                BiomeGroup[] groupB = biomesBatched2.GetValue(biomesMain) as BiomeGroup[];
                if (groupB == null || groupB.Length == 0)
                {
                    DebugWater.Log("Could not change biomes: BiomeGroups was not loaded...");
                    return;
                }
                ready = true;
                ManWorld.inst.TileManager.PauseGenerationOneFrame();
                biomesMain.InvalidateBiomeDB();
                //DebugWater.Log("Biomes: " + biomesMain.GetNumBiomes());
                List<BiomeGroup> biomesGrouped = groupB.ToList();
                var biomesDataGet = biomesData.GetValue(biomesMain);
                errorCode++;
                List<Biome> biomes = null;
                if (!appended)
                {
                    if (biomesDataGet == null || biomesAll.GetValue(biomesDataGet) == null)
                    {
                        biomes = new List<Biome>();
                        foreach (BiomeGroup biomeGroup in biomesGrouped)
                        {
                            for (int step = 0; step < biomeGroup.Biomes.Length; step++)
                            {
                                Biome biom = biomeGroup.Biomes[step];
                                if (!biomes.Contains(biom))
                                    biomes.Add(biom);
                            }
                        }
                        /*
                        DebugWater.Log("Waiting on biomesDataGet to load...");
                        return;
                        */
                    }
                    else
                        biomes = ((Biome[])biomesAll.GetValue(biomesDataGet)).ToList();
                    errorCode++;
                    if (Seabiomes == null)
                    {
                        Seabiomes = new Dictionary<Biome, float>();
                        errorCode++;
                        for (int step = 0; step < biomes.Count; step++)
                        {
                            var item = biomes[step];
                            if (item.name == "MuddyPondBiome")
                            {
                                DebugWater.LogGen("Biomes ready!");
                                return;
                            }
                        }
                        errorCode++;
                        DebugWater.LogGen("Generating new biomes...");
                        for (int step = 0; step < biomes.Count; step++)
                        {
                            var item = biomes[step];
                            DebugWater.LogGen("Biome #" + step + " - " + item.name);
                        }
                        errorCode = 300;
                        foreach (var item in ApplyOceanicBiomes(biomes))
                        {
                            Seabiomes.Add(item.Key, item.Value);
                            errorCode++;
                        }

                        //if (biomesDataGet != null)
                        //    biomesAll.SetValue(biomesDataGet, biomes.ToArray());
                        //errorCode++;
                        //biomesAll2.SetValue(biomesMain, biomes.ToArray());
                        Biome[] biomeShoehorn = new Biome[Seabiomes.Count];
                        float[] biomeWeights = new float[Seabiomes.Count];
                        for (int i = 0; i < Seabiomes.Count; i++)
                        {
                            var stepC = Seabiomes.ElementAt(i);
                            biomeShoehorn[i] = stepC.Key;
                            biomeWeights[i] = stepC.Value;
                        }
                        seaBiomeGroup = CopyBiomeGroup(biomesGrouped.First(),
                        "SeaBiomeGroup", biomeShoehorn, biomeWeights);
                        AnimationCurve AC = AnimationCurve.Linear(0f, 1f, 100f, 0f); //new AnimationCurve();
                        distWeight.SetValue(seaBiomeGroup, AC);

                        foreach (var item in Seabiomes)
                        {
                            biomes.Add(item.Key);
                        }
                    }

                    errorCode = 15000;
                    biomesGrouped.Add(seaBiomeGroup);

                    errorCode++;
                    if (ApplySeaToALL)
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

                                Array.Resize(ref biomes2, biomes2.Length + Seabiomes.Count);
                                for (int step2 = 0; step2 < Seabiomes.Count; step2++)
                                {
                                    biomes2[biomes2.Length - (step2 + 1)] = Seabiomes.ElementAt(step2).Key;
                                }
                                biomesInside.SetValue(item, biomes2);
                                DebugWater.LogGen("Biomes added!");

                                float[] biomesWeightsCached = (float[])biomesWeights.GetValue(item);
                                if (biomesWeightsCached == null)
                                {
                                    DebugWater.Log("NULL Biome Weights?");
                                    continue;
                                }

                                Array.Resize(ref biomesWeightsCached, biomesWeightsCached.Length + Seabiomes.Count);
                                for (int step2 = 0; step2 < Seabiomes.Count; step2++)
                                {
                                    biomesWeightsCached[biomes2.Length - (step2 + 1)] = Seabiomes.ElementAt(step2).Value;
                                }
                                biomesWeights.SetValue(item, biomesWeightsCached);
                                DebugWater.LogGen("Biome weights added!");
                                for (int step2 = 0; step2 < biomes2.Length; step2++)
                                {
                                    DebugWater.LogGen("Biome " + (biomes2[step2].name.NullOrEmpty() ? "<NULL>" : biomes2[step2].name) +
                                        " - " + biomesWeightsCached[step2].ToString());
                                }
                            }
                            catch (Exception e) { DebugWater.Log("error " + e); }
                        }
                    }
                    else
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
                                KeyValuePair<Biome, float> newBiome = TryGetPondBiome(item.name, biomes);
                                if (newBiome.Key != null)
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

                                    for (int step2 = 0; step2 < biomes2.Length; step2++)
                                    {
                                        DebugWater.Log("- Biome " + (biomes2[step2].name.NullOrEmpty() ? "<NULL>" : biomes2[step2].name) +
                                            " - " + biomesWeightsCached[step2].ToString());
                                    }

                                    Array.Resize(ref biomes2, biomes2.Length + 1);
                                    biomes2[biomes2.Length - 1] = newBiome.Key;
                                    biomesInside.SetValue(item, biomes2);
                                    DebugWater.LogGen("Biomes added!");

                                    Array.Resize(ref biomesWeightsCached, biomesWeightsCached.Length + 1);
                                    biomesWeightsCached[biomes2.Length - 1] = newBiome.Value;
                                    biomesWeights.SetValue(item, biomesWeightsCached);
                                    DebugWater.LogGen("Biome weights added!");
                                }
                            }
                            catch (Exception e) { DebugWater.Log("error " + e); }
                        }
                    }

                    errorCode++;
                    if (biomesDataGet != null)
                        biomesBatched.SetValue(biomesDataGet, biomesGrouped.ToArray());
                    errorCode++;
                    biomesBatched2.SetValue(biomesMain, biomesGrouped.ToArray());
                    DebugWater.Log("Ocean Biomes setup!");
                    appended = true;
                }
                else
                {
                    if (ApplySeaToALL)
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

                            for (int step2 = 0; step2 < Seabiomes.Count; step2++)
                            {
                                biomesWeightsCached[biomes2.Length - (step2 + 1)] = Seabiomes.ElementAt(step2).Value;
                            }
                            biomesWeights.SetValue(item, biomesWeightsCached);
                        }
                    }
                    else
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
                                KeyValuePair<Biome, float> newBiome = TryGetPondBiome(item.name, biomes);
                                if (newBiome.Key != null)
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

                                    for (int step2 = 0; step2 < biomes2.Length; step2++)
                                    {
                                        DebugWater.Log("- Biome " + (biomes2[step2].name.NullOrEmpty() ? "<NULL>" : biomes2[step2].name) +
                                            " - " + biomesWeightsCached[step2].ToString());
                                    }

                                    biomes2[biomes2.Length - 1] = newBiome.Key;
                                    biomesInside.SetValue(item, biomes2);
                                    DebugWater.LogGen("Biomes added!");

                                    Array.Resize(ref biomesWeightsCached, biomesWeightsCached.Length + 1);
                                    biomesWeightsCached[biomes2.Length - 1] = newBiome.Value;
                                    biomesWeights.SetValue(item, biomesWeightsCached);
                                    DebugWater.LogGen("Biome weights added!");
                                }
                            }
                            catch (Exception e) { DebugWater.Log("error " + e); }
                        }
                    }
                    biomesGrouped.Add(seaBiomeGroup);
                    errorCode++;
                    biomesBatched2.SetValue(biomesMain, biomesGrouped.ToArray());
                    DebugWater.Log("Ocean Biomes reloaded");
                }
                ManWorld.inst.TileManager.Reset();
            }
            catch (Exception e)
            {
                throw new Exception("Failed at " + errorCode, e);
            }
        }

        public static void RemoveOceanicBiomes(BiomeMap biomesMain)
        {
            if (!applied)
                return;
            int errorCode = 0;
            applied = false;
            try
            {
                TerrainOperations.BeachingMode = false;
                DebugWater.Log("Ocean man why don't you take me by the hand~");
                if (biomesMain == null)
                {
                    DebugWater.Log("Could not change biomes: Biomes were not loaded...");
                    return;
                }
                if (ManWorld.inst?.TileManager == null)
                {
                    DebugWater.Log("Could not change biomes: TileManager was not loaded...");
                    return;
                }
                BiomeGroup[] groupB = biomesBatched2.GetValue(biomesMain) as BiomeGroup[];
                if (groupB == null || groupB.Length == 0)
                {
                    DebugWater.Log("Could not change biomes: BiomeGroups was not loaded...");
                    return;
                }
                OnClampTerrain.Unsubscribe(CleanupMess);
                ready = false;
                ManWorld.inst.TileManager.PauseGenerationOneFrame();
                biomesMain.InvalidateBiomeDB();
                //DebugWater.Log("Biomes: " + biomesMain.GetNumBiomes());
                List<BiomeGroup> biomesGrouped = groupB.ToList();
                if (ApplySeaToALL)
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
                        for (int step2 = 0; step2 < Seabiomes.Count; step2++)
                        {
                            biomesWeightsCached[biomes2.Length - (step2 + 1)] = 0;
                        }
                        biomesWeights.SetValue(item, biomesWeightsCached);
                    }
                }
                biomesGrouped.Remove(seaBiomeGroup);

                errorCode++;
                biomesBatched2.SetValue(biomesMain, biomesGrouped.ToArray());
                DebugWater.Log("Ocean Biomes removed");
                ManWorld.inst.TileManager.Reset();
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
