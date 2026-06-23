namespace Dungeon
{
    /// <summary>
    /// Индексы спрайтов в TX_Tileset_Wall (32×32 px, нумерация слева-направо сверху-вниз).
    /// Используются в DungeonWallTileSet для создания тайлов через Sprite.
    /// </summary>
    public static class WallTileIndex
    {
        // ──────────────────────────────────────────
        // ТИП 1 — коробка снаружи (игрок снаружи смотрит на лицевую стену)
        // Порядок слоёв сверху вниз:
        //   WallTop → Roof → WallFace_Top → WallFace_Bot → Floor
        // ──────────────────────────────────────────

        // Верхний левый угол типа 1 (2 тайла: верх + низ)
        public const int T1_CornerTopLeft_Top    = 0;
        public const int T1_CornerTopLeft_Bot    = 53;

        // Верхняя стена типа 1 (варианты для разнообразия)
        public const int T1_WallTop_A            = 19;
        public const int T1_WallTop_B            = 20;
        public const int T1_WallTop_C            = 21;
        public const int T1_WallTop_D            = 50;
        public const int T1_WallTop_E            = 51;
        public const int T1_WallTop_F            = 52;

        // Верхний правый угол типа 1 (2 тайла)
        public const int T1_CornerTopRight_Top   = 2;
        public const int T1_CornerTopRight_Bot   = 54;

        // Правая боковая стена коробки типа 1 (3 варианта)
        public const int T1_WallRight_A          = 9;
        public const int T1_WallRight_B          = 47;
        public const int T1_WallRight_C          = 48;
        public const int T1_WallRight_D          = 49;

        // Левая боковая стена коробки типа 1 (3 варианта)
        public const int T1_WallLeft_A           = 8;
        public const int T1_WallLeft_B           = 44;
        public const int T1_WallLeft_C           = 45;
        public const int T1_WallLeft_D           = 46;

        // Левая угловая лицевая стена — верхняя часть типа 1
        public const int T1_CornerFaceLeft_Top   = 15;
        public const int T1_CornerFaceLeft_Bot   = 57;

        // Правая угловая лицевая стена — нижняя часть типа 1
        public const int T1_CornerFaceRight_Top  = 23;
        public const int T1_CornerFaceRight_Bot  = 59;

        // ──────────────────────────────────────────
        // ОБЩИЕ стены — используются в обоих типах
        // Для типа 1: лицевая стена (WallFace_Top / WallFace_Bot)
        // Для типа 2: задняя стена (вид изнутри)
        // ──────────────────────────────────────────

        // Верхняя часть стены (WallFace_Top для т1 / задняя верхняя для т2)
        public const int WallUpper_A             = 16;
        public const int WallUpper_B             = 5;
        public const int WallUpper_C             = 28;
        public const int WallUpper_D             = 29;
        public const int WallUpper_E             = 30;
        public const int WallUpper_F             = 31;
        public const int WallUpper_G             = 36;
        public const int WallUpper_H             = 38;
        public const int WallUpper_I             = 40;
        public const int WallUpper_J             = 42;

        // Нижняя часть стены (WallFace_Bot для т1 / задняя нижняя для т2)
        public const int WallLower_A             = 24;
        public const int WallLower_B             = 12;
        public const int WallLower_C             = 32;
        public const int WallLower_D             = 33;
        public const int WallLower_E             = 34;
        public const int WallLower_F             = 35;
        public const int WallLower_G             = 37;
        public const int WallLower_H             = 39;
        public const int WallLower_I             = 41;
        public const int WallLower_J             = 43;

        // ──────────────────────────────────────────
        // ТИП 2 — коробка изнутри (игрок внутри, видит верх и боковые изнутри)
        // Порядок слоёв: WallTop_Top → WallTop_Bot → FloorInner → WallBot
        // ──────────────────────────────────────────

        // Верхний левый угол типа 2 (1 тайл)
        public const int T2_CornerTopLeft        = 3;

        // Верхний правый угол типа 2 (1 тайл)
        // (зеркало T2_CornerTopLeft, добавить если есть отдельный спрайт)

        // Верх левой боковой стены изнутри (примыкает к верхнему левому углу)
        public const int T2_WallLeft_Top         = 4;
        // Низ левой боковой стены изнутри (примыкает к боковой стене)
        public const int T2_WallLeft_Bot         = 11;

        // Верх правой боковой стены изнутри
        public const int T2_WallRight_Top        = 6;
        // Низ правой боковой стены изнутри
        public const int T2_WallRight_Bot        = 13;

        // Правый нижний угол типа 2
        public const int T2_CornerBotRight       = 22;
        // Левый нижний угол типа 2
        public const int T2_CornerBotLeft        = 18;

        // Нижние стены из одного тайла для типа 2 (варианты)
        public const int T2_WallBot_A            = 19;
        public const int T2_WallBot_B            = 20;
        public const int T2_WallBot_C            = 21;
        public const int T2_WallBot_D            = 1;
        public const int T2_WallBot_E            = 50;
        public const int T2_WallBot_F            = 51;
        public const int T2_WallBot_G            = 52;

        // ──────────────────────────────────────────
        // БОКОВЫЕ СТЕНЫ (общие)
        // 44,45,46 → для т1 левые / для т2 правые
        // 47,48,49 → для т1 правые / для т2 левые
        // ──────────────────────────────────────────
        public const int SideWall_L_A            = 44;
        public const int SideWall_L_B            = 45;
        public const int SideWall_L_C            = 46;

        public const int SideWall_R_A            = 47;
        public const int SideWall_R_B            = 48;
        public const int SideWall_R_C            = 49;
    }
}