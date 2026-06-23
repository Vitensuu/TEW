namespace Dungeon
{
    /// <summary>
    /// Индексы спрайтов в TX_Tileset_Wall (32×32 px, нумерация слева-направо сверху-вниз).
    ///
    /// ═══ ТИП 1 (снаружи) ═══
    ///   тайл1.1  = 0              — верхний левый угол (верх)
    ///   тайл1.1  = 53             — верхний левый угол (низ, пара к 0)
    ///   тайл1.2  = 19,20,21,50,51,52 — самый верх
    ///   тайл1.3  = 2              — верхний правый угол (верх)
    ///   тайл1.3  = 54             — верхний правый угол (низ, пара к 2)
    ///   тайл2.1  = 44,45,46,8    — левая стена коробки
    ///   тайл2.3  = 47,48,49,9    — правая стена коробки
    ///   тайл3.1  = 15, 57        — левая угловая лицевая верхняя
    ///   тайл3.2  = 16,5,28..42  — стена верхняя (лицевая)
    ///   тайл3.3  = 17, 58        — правая угловая лицевая верхняя
    ///   тайл4.1  = 23, 59        — левая угловая лицевая нижняя
    ///   тайл4.2  = 24,12,32..43 — стена нижняя (лицевая)
    ///   тайл4.3  = 25, 60        — правая угловая лицевая нижняя
    ///
    /// ═══ ТИП 2 (изнутри) ═══
    ///   тайл1.1  = 3              — верхний левый угол
    ///   тайл1.2  = 4              — верх левой стены изнутри (примыкает к углу)
    ///   тайл1.3  = 16,5,28..42  — стена верхняя (общая с тип1)
    ///   тайл1.4  = 6              — верх правой стены изнутри (примыкает к углу)
    ///   тайл1.5  = 7              — верхний правый угол
    ///   тайл2.1  = 44,45,46,8    — боковая правая стена (для тип2!)
    ///   тайл2.2  = 11             — низ левой стены изнутри (примыкает к боковой)
    ///   тайл2.3  = 24,12,32..43 — стена нижняя (общая с тип1)
    ///   тайл2.4  = 13             — низ правой стены изнутри (примыкает к боковой)
    ///   тайл2.5  = 47,48,49,9    — боковая левая стена (для тип2!)
    ///   тайл3.1  = 44,45,46,8    — боковая правая стена (для тип2!)
    ///   тайл3.5  = 47,48,49,9    — боковая левая стена (для тип2!)
    ///   тайл4.1  = 18             — левый нижний угол
    ///   тайл4.2-4.4 = 24,12,32,33,34,35,37,39,41,43 — нижняя стена (общая с тип1 лицевой низ!)
    ///   тайл4.5  = 22             — правый нижний угол
    ///
    /// ВАЖНО: боковые стены для тип2 ПЕРЕВЁРНУТЫ относительно тип1:
    ///   44,45,46,8 = для тип1 ЛЕВЫЕ,  для тип2 ПРАВЫЕ
    ///   47,48,49,9 = для тип1 ПРАВЫЕ, для тип2 ЛЕВЫЕ
    /// </summary>
    public static class WallTileIndex
    {
        // ──────────────────────────────────────────
        // ТИП 1 — верх коробки (строка 1)
        // ──────────────────────────────────────────
        public const int T1_CornerTopLeft_Top    = 0;    // тайл1.1 верх
        public const int T1_CornerTopLeft_Bot    = 53;   // тайл1.1 низ (пара)
        public const int T1_WallTop_A            = 19;   // тайл1.2 варианты
        public const int T1_WallTop_B            = 20;
        public const int T1_WallTop_C            = 21;
        public const int T1_WallTop_D            = 50;
        public const int T1_WallTop_E            = 51;
        public const int T1_WallTop_F            = 52;
        public const int T1_CornerTopRight_Top   = 2;    // тайл1.3 верх
        public const int T1_CornerTopRight_Bot   = 54;   // тайл1.3 низ (пара)

        // ──────────────────────────────────────────
        // ТИП 1 — боковые стены коробки (строка 2)
        // ──────────────────────────────────────────
        public const int T1_WallLeft_A           = 44;   // тайл2.1
        public const int T1_WallLeft_B           = 45;
        public const int T1_WallLeft_C           = 46;
        public const int T1_WallLeft_D           = 8;
        public const int T1_WallRight_A          = 47;   // тайл2.3
        public const int T1_WallRight_B          = 48;
        public const int T1_WallRight_C          = 49;
        public const int T1_WallRight_D          = 9;

        // ──────────────────────────────────────────
        // ТИП 1 — лицевая стена верх (строка 3)
        // ──────────────────────────────────────────
        public const int T1_FaceLeft_Top         = 15;   // тайл3.1
        public const int T1_FaceLeft_Top2        = 57;
        public const int T1_FaceRight_Top        = 17;   // тайл3.3
        public const int T1_FaceRight_Top2       = 58;

        // ──────────────────────────────────────────
        // ТИП 1 — лицевая стена низ (строка 4)
        // ──────────────────────────────────────────
        public const int T1_FaceLeft_Bot         = 23;   // тайл4.1
        public const int T1_FaceLeft_Bot2        = 59;
        public const int T1_FaceRight_Bot        = 25;   // тайл4.3
        public const int T1_FaceRight_Bot2       = 60;

        // ──────────────────────────────────────────
        // ОБЩАЯ стена верхняя (тайл3.2 тип1 = тайл1.3 тип2)
        //   idx: 16, 5, 28, 29, 30, 31, 36, 38, 40, 42
        // ──────────────────────────────────────────
        public const int WallFace_Top_A          = 16;
        public const int WallFace_Top_B          = 5;
        public const int WallFace_Top_C          = 28;
        public const int WallFace_Top_D          = 29;
        public const int WallFace_Top_E          = 30;
        public const int WallFace_Top_F          = 31;
        public const int WallFace_Top_G          = 36;
        public const int WallFace_Top_H          = 38;
        public const int WallFace_Top_I          = 40;
        public const int WallFace_Top_J          = 42;

        // ──────────────────────────────────────────
        // ОБЩАЯ стена нижняя (тайл4.2 тип1 = тайл2.3 тип2)
        //   idx: 24, 12, 32, 33, 34, 35, 37, 39, 41, 43
        // ──────────────────────────────────────────
        public const int WallFace_Bot_A          = 24;
        public const int WallFace_Bot_B          = 12;
        public const int WallFace_Bot_C          = 32;
        public const int WallFace_Bot_D          = 33;
        public const int WallFace_Bot_E          = 34;
        public const int WallFace_Bot_F          = 35;
        public const int WallFace_Bot_G          = 37;
        public const int WallFace_Bot_H          = 39;
        public const int WallFace_Bot_I          = 41;
        public const int WallFace_Bot_J          = 43;

        // ──────────────────────────────────────────
        // ТИП 2 — строка 1: верхний ряд изнутри
        // ──────────────────────────────────────────
        public const int T2_CornerTopLeft        = 3;    // тайл1.1
        public const int T2_WallLeft_Top         = 4;    // тайл1.2
        // тайл1.3 = WallFace_Top_* (общие)
        public const int T2_WallRight_Top        = 6;    // тайл1.4
        public const int T2_CornerTopRight       = 7;    // тайл1.5

        // ──────────────────────────────────────────
        // ТИП 2 — строка 2: переходный ряд боковых стен
        // ──────────────────────────────────────────
        // тайл2.1 / тайл3.1 = T2_SideRight_* (44,45,46,8)
        public const int T2_WallLeft_Bot         = 11;   // тайл2.2
        // тайл2.3 = WallFace_Bot_* (общие)
        public const int T2_WallRight_Bot        = 13;   // тайл2.4
        // тайл2.5 / тайл3.5 = T2_SideLeft_* (47,48,49,9)

        // ──────────────────────────────────────────
        // ТИП 2 — боковые стены (строки 2-3)
        //   НАОБОРОТ относительно тип1!
        // ──────────────────────────────────────────
        public const int T2_SideRight_A          = 44;   // тайл2.1/3.1
        public const int T2_SideRight_B          = 45;
        public const int T2_SideRight_C          = 46;
        public const int T2_SideRight_D          = 8;
        public const int T2_SideLeft_A           = 47;   // тайл2.5/3.5
        public const int T2_SideLeft_B           = 48;
        public const int T2_SideLeft_C           = 49;
        public const int T2_SideLeft_D           = 9;

        // ──────────────────────────────────────────
        // ТИП 2 — строка 4: нижний ряд изнутри
        // ──────────────────────────────────────────
        public const int T2_CornerBotLeft        = 18;   // тайл4.1
        // тайл4.2-4.4 = WallFace_Bot_* (те же что тип1 лицевая низ: 24,12,32..43)
        public const int T2_CornerBotRight       = 22;   // тайл4.5

        // ──────────────────────────────────────────
        // КОРИДОРЫ — боковые стены общие
        // ──────────────────────────────────────────
        public const int SideWall_L_A            = 44;
        public const int SideWall_L_B            = 45;
        public const int SideWall_L_C            = 46;
        public const int SideWall_L_D            = 8;
        public const int SideWall_R_A            = 47;
        public const int SideWall_R_B            = 48;
        public const int SideWall_R_C            = 49;
        public const int SideWall_R_D            = 9;
    }
}