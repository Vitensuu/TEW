using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Dungeon;

namespace Game.UI
{
    /// <summary>
    /// Мини-карта с туманом войны (ТЗ ЭТАП 6 — Room Graph для миникарты).
    /// Рисует по иконке на узел RoomGraph, нормируя мировые позиции комнат.
    /// Открывает комнату, когда узел помечен Visited (RoomEncounter), рисует
    /// связи между соседями. Полностью на новой архитектуре (RoomManager).
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [SerializeField] RectTransform mapContainer;
        [SerializeField] GameObject roomIconPrefab;   // UI Image; null → создаём простой
        [SerializeField] float iconSize = 14f;
        [SerializeField] float mapScale = 0.5f;
        [SerializeField] bool revealAll = false;       // для отладки

        readonly Dictionary<RoomNode, Image> _icons = new Dictionary<RoomNode, Image>();
        RoomGraph _graph;
        Vector2 _worldCenter;

        float _rebuildTimer;

        void Update()
        {
            // Подхватываем новый этаж (граф мог пересобраться).
            var graph = RoomManager.Instance != null ? RoomManager.Instance.Graph : null;
            if (graph != _graph) { _graph = graph; Rebuild(); }

            RefreshFog();
        }

        void Rebuild()
        {
            if (mapContainer == null) return;
            foreach (Transform c in mapContainer) Destroy(c.gameObject);
            _icons.Clear();
            if (_graph == null || _graph.Nodes.Count == 0) return;

            // Центр карты — средняя мировая позиция комнат.
            _worldCenter = Vector2.zero;
            int n = 0;
            foreach (var node in _graph.Nodes)
                if (node.Instance != null) { _worldCenter += (Vector2)node.Instance.transform.position; n++; }
            if (n > 0) _worldCenter /= n;

            foreach (var node in _graph.Nodes)
            {
                if (node.Instance == null) continue;
                var img = CreateIcon();
                var rt = img.rectTransform;
                rt.sizeDelta = Vector2.one * iconSize;
                rt.anchoredPosition =
                    ((Vector2)node.Instance.transform.position - _worldCenter) * mapScale;
                _icons[node] = img;
            }
        }

        void RefreshFog()
        {
            foreach (var kv in _icons)
            {
                bool show = revealAll || kv.Key.Visited || kv.Key.Type == RoomType.Start;
                kv.Value.color = show ? ColorFor(kv.Key.Type) : new Color(1, 1, 1, 0.10f);
            }
        }

        Image CreateIcon()
        {
            GameObject go;
            if (roomIconPrefab != null) go = Instantiate(roomIconPrefab, mapContainer);
            else
            {
                go = new GameObject("RoomIcon", typeof(Image));
                go.transform.SetParent(mapContainer, false);
            }
            return go.GetComponent<Image>();
        }

        static Color ColorFor(RoomType t) => t switch
        {
            RoomType.Start    => Color.cyan,
            RoomType.Boss     => Color.red,
            RoomType.Treasure => Color.yellow,
            RoomType.Shop     => Color.green,
            RoomType.Shrine   => new Color(0.6f, 0.8f, 1f),
            RoomType.Elite    => Color.magenta,
            RoomType.Exit     => Color.blue,
            RoomType.Secret   => new Color(0.5f, 0.2f, 0.6f),
            _                 => Color.white,
        };
    }
}
