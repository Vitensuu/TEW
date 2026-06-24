// Положи этот файл в: Assets/Editor/WallTileSetAutoFill.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Editor
{
    public static class WallTileSetAutoFill
    {
        private const string TilesFolder = "Assets/Tiles/Tiles/Wall";

        [MenuItem("Assets/Auto-Fill Wall Tile Set", true)]
        private static bool ValidateAutoFill()
            => Selection.activeObject is DungeonWallTileSet;

        [MenuItem("Assets/Auto-Fill Wall Tile Set")]
        [MenuItem("Dungeon/Auto-Fill Wall Tile Set")]
        private static void AutoFill()
        {
            var ts = Selection.activeObject as DungeonWallTileSet;
            if (ts == null)
            {
                var guids = AssetDatabase.FindAssets("t:DungeonWallTileSet");
                if (guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("Ошибка",
                        "Выдели DungeonWallTileSet в Project и повтори.", "OK");
                    return;
                }
                ts = AssetDatabase.LoadAssetAtPath<DungeonWallTileSet>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            Undo.RecordObject(ts, "Auto-Fill Wall Tile Set");

            // ── ТИП 1 — строка 1: верх коробки ──────────────────────────────
            ts.t1_CornerTopLeft  = Load("TX Tileset Wall_0");
            ts.t1_WallTop = new TileBase[]
            {
                Load("TX Tileset Wall_19"),
                Load("TX Tileset Wall_20"),
                Load("TX Tileset Wall_21"),
                Load("TX Tileset Wall_50"),
                Load("TX Tileset Wall_51"),
                Load("TX Tileset Wall_52"),
            };
            ts.t1_CornerTopRight = Load("TX Tileset Wall_2");

            // ── ТИП 1 — строка 2: боковые стены ─────────────────────────────
            ts.t1_WallLeft = new TileBase[]
            {
                Load("TX Tileset Wall_8"),
                Load("TX Tileset Wall_44"),
                Load("TX Tileset Wall_45"),
                Load("TX Tileset Wall_46"),
            };
            ts.t1_WallRight = new TileBase[]
            {
                Load("TX Tileset Wall_9"),
                Load("TX Tileset Wall_47"),
                Load("TX Tileset Wall_48"),
                Load("TX Tileset Wall_49"),
            };

            // ── ТИП 1 — строка 3: лицевая верх ──────────────────────────────
            ts.t1_FaceLeft_Top  = Load("TX Tileset Wall_15");
            ts.t1_FaceLeft_Top2 = Load("TX Tileset Wall_57");
            ts.t1_WallFace_Top = new TileBase[]
            {
                Load("TX Tileset Wall_16"),
                Load("TX Tileset Wall_5"),
                Load("TX Tileset Wall_28"),
                Load("TX Tileset Wall_29"),
                Load("TX Tileset Wall_30"),
                Load("TX Tileset Wall_31"),
                Load("TX Tileset Wall_36"),
                Load("TX Tileset Wall_38"),
                Load("TX Tileset Wall_40"),
                Load("TX Tileset Wall_42"),
            };
            ts.t1_FaceRight_Top  = Load("TX Tileset Wall_17");
            ts.t1_FaceRight_Top2 = Load("TX Tileset Wall_58");

            // ── ТИП 1 — строка 4: лицевая низ ───────────────────────────────
            ts.t1_FaceLeft_Bot  = Load("TX Tileset Wall_23");
            ts.t1_FaceLeft_Bot2 = Load("TX Tileset Wall_59");
            ts.t1_WallFace_Bot = new TileBase[]
            {
                Load("TX Tileset Wall_24"),
                Load("TX Tileset Wall_12"),
                Load("TX Tileset Wall_32"),
                Load("TX Tileset Wall_33"),
                Load("TX Tileset Wall_34"),
                Load("TX Tileset Wall_35"),
                Load("TX Tileset Wall_37"),
                Load("TX Tileset Wall_39"),
                Load("TX Tileset Wall_41"),
                Load("TX Tileset Wall_43"),
            };
            ts.t1_FaceRight_Bot  = Load("TX Tileset Wall_25");
            ts.t1_FaceRight_Bot2 = Load("TX Tileset Wall_60");

            // ── ТИП 1 — нижние части верхних угловых тайлов ─────────────────
            ts.t1_CornerTopLeft_Bot  = Load("TX Tileset Wall_53");
            ts.t1_CornerTopRight_Bot = Load("TX Tileset Wall_54");

            // ── ТИП 2 — строка 1: верхний ряд изнутри ───────────────────────
            // тайл1.1 = 3, тайл1.2 = 4, тайл1.3 = общие WallFace_Top,
            // тайл1.4 = 6, тайл1.5 = 7
            ts.t2_CornerTopLeft  = Load("TX Tileset Wall_3");
            ts.t2_WallLeft_Top   = Load("TX Tileset Wall_4");
            // t2 тайл1.3 — использует t1_WallFace_Top (уже заполнен выше)
            ts.t2_WallRight_Top  = Load("TX Tileset Wall_6");
            ts.t2_CornerTopRight = Load("TX Tileset Wall_7");

            // ── ТИП 2 — строка 2: переходный ряд боковых стен ───────────────
            // тайл2.2 = 11, тайл2.3 = общие WallFace_Bot, тайл2.4 = 13
            ts.t2_WallLeft_Bot   = Load("TX Tileset Wall_11");
            // t2 тайл2.3 — использует t1_WallFace_Bot (уже заполнен выше)
            ts.t2_WallRight_Bot  = Load("TX Tileset Wall_13");

            // ── ТИП 2 — строки 2-3: боковые стены ───────────────────────────
            // ВНИМАНИЕ: для тип2 направление ОБРАТНОЕ относительно тип1!
            // т2_SideRight (пол слева, стена справа) = idx 44,45,46,8
            // т2_SideLeft  (пол справа, стена слева) = idx 47,48,49,9
            ts.t2_SideRight = new TileBase[]
            {
                Load("TX Tileset Wall_44"),
                Load("TX Tileset Wall_45"),
                Load("TX Tileset Wall_46"),
                Load("TX Tileset Wall_8"),
            };
            ts.t2_SideLeft = new TileBase[]
            {
                Load("TX Tileset Wall_47"),
                Load("TX Tileset Wall_48"),
                Load("TX Tileset Wall_49"),
                Load("TX Tileset Wall_9"),
            };

            // ── ТИП 2 — строка 4: нижний ряд изнутри ────────────────────────
            ts.t2_CornerBotLeft  = Load("TX Tileset Wall_18");
            ts.t2_WallBot = new TileBase[]
            {
                Load("TX Tileset Wall_1"),
                Load("TX Tileset Wall_19"),
                Load("TX Tileset Wall_20"),
                Load("TX Tileset Wall_21"),
                Load("TX Tileset Wall_50"),
                Load("TX Tileset Wall_51"),
                Load("TX Tileset Wall_52"),
            };
            ts.t2_CornerBotRight = Load("TX Tileset Wall_22");

            // ── КОРИДОРЫ — боковые стены общие ───────────────────────────────
            ts.sideWall_L = new TileBase[]
            {
                Load("TX Tileset Wall_44"),
                Load("TX Tileset Wall_45"),
                Load("TX Tileset Wall_46"),
                Load("TX Tileset Wall_8"),
            };
            ts.sideWall_R = new TileBase[]
            {
                Load("TX Tileset Wall_47"),
                Load("TX Tileset Wall_48"),
                Load("TX Tileset Wall_49"),
                Load("TX Tileset Wall_9"),
            };

            EditorUtility.SetDirty(ts);
            AssetDatabase.SaveAssets();

            int missing = CountMissing(ts);
            string msg = missing == 0
                ? "Все тайлы найдены!"
                : $"{missing} тайл(ов) не найдено. Проверь Console.";
            EditorUtility.DisplayDialog("Auto-Fill Wall Tile Set", msg, "OK");
            Debug.Log($"[WallTileSetAutoFill] Готово. Папка: {TilesFolder}");
        }

        private static TileBase Load(string name)
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TilesFolder}/{name}.asset");
            if (tile != null) return tile;

            foreach (var guid in AssetDatabase.FindAssets($"{name} t:TileBase"))
            {
                var t = AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(guid));
                if (t != null && t.name == name) return t;
            }

            Debug.LogWarning($"[WallTileSetAutoFill] Не найден: {name}");
            return null;
        }

        private static int CountMissing(DungeonWallTileSet ts)
        {
            int n = 0;
            void Check(TileBase t) { if (!t) n++; }
            void CheckArr(TileBase[] arr) { if (arr != null) foreach (var t in arr) if (!t) n++; }

            // ТИП 1
            Check(ts.t1_CornerTopLeft);     Check(ts.t1_CornerTopRight);
            Check(ts.t1_CornerTopLeft_Bot); Check(ts.t1_CornerTopRight_Bot);
            Check(ts.t1_FaceLeft_Top);      Check(ts.t1_FaceLeft_Top2);
            Check(ts.t1_FaceRight_Top);     Check(ts.t1_FaceRight_Top2);
            Check(ts.t1_FaceLeft_Bot);      Check(ts.t1_FaceLeft_Bot2);
            Check(ts.t1_FaceRight_Bot);     Check(ts.t1_FaceRight_Bot2);
            CheckArr(ts.t1_WallTop);
            CheckArr(ts.t1_WallLeft);       CheckArr(ts.t1_WallRight);
            CheckArr(ts.t1_WallFace_Top);   CheckArr(ts.t1_WallFace_Bot);

            // ТИП 2
            Check(ts.t2_CornerTopLeft);     Check(ts.t2_CornerTopRight);
            Check(ts.t2_WallLeft_Top);      Check(ts.t2_WallRight_Top);
            Check(ts.t2_WallLeft_Bot);      Check(ts.t2_WallRight_Bot);
            Check(ts.t2_CornerBotLeft);     Check(ts.t2_CornerBotRight);
            CheckArr(ts.t2_SideRight);      CheckArr(ts.t2_SideLeft);
            CheckArr(ts.t2_WallBot);

            // КОРИДОРЫ
            CheckArr(ts.sideWall_L);        CheckArr(ts.sideWall_R);

            return n;
        }
    }
}
#endif