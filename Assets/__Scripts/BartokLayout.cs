using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // Делает SlotDef видимым в инспекторе Unity
public class SlotDef
{
    public float        x;
    public float        y;
    public bool         faceUp = false;
    public string       layerName = "Default";
    public int          layerID = 0;
    public int          id;
    public List<int>    hiddenBy = new List<int>();
    public float        rot; // Поворот в зависимости от игрока
    public string       type = "slot";
    public Vector2      stagger;
    public int          player; // Порядковый номер игрока
    public Vector3      pos;    // Вычисляется на основе х,у и multiplier
}

public class BartokLayout : MonoBehaviour
{
    [Header("Set Dynamically")]
    public PT_XMLReader     xmlr;
    public PT_XMLHashtable  xml; // Используется для ускорения доступа к xml 
    public Vector2          multiplier; // Смещение в раскладке
    // Ссылки на SlotDef
    public List<SlotDef>    slotDefs;
    public SlotDef          drawPile;
    public SlotDef          discardPile;
    public SlotDef          target;

    // Этот метод вызывается для чтения файла BartokLayoutXML.xml
    public void ReadLayout(string xmlText) 
    {
        xmlr = new PT_XMLReader();
        xmlr.Parse(xmlText);        // Загрузить XML
        xml = xmlr.xml["xml"][0];   // И определить xml для ускорения доступа к XML

        // Прочитать множители, определяющие расстояние между картами
        multiplier.x = float.Parse(xml["multiplier"][0].att("x"));
        multiplier.y = float.Parse(xml["multiplier"][0].att("y"));

        // Прочитать слоты
        SlotDef tSD;
        // slotsX используется для ускорения доступа к элементам <slot>
        PT_XMLHashList slotsX = xml["slot"];

        for (int i=0; i<slotsX.Count; i++) {
            tSD = new SlotDef(); // Создать новый экземпляр SlotDef
            if (slotsX[i].HasAtt("type")) { // Если имеет атрибут type, прочитать его
                tSD.type = slotsX[i].att("type");
            } else { // Иначе определить тип как "slot". Это отдельная карта в ряду
                tSD.type = "slot";
            }
            // Преобразовать некоторые атрибуты в числовые значения
            tSD.x = float.Parse(slotsX[i].att("x"));
            tSD.y = float.Parse(slotsX[i].att("y"));
            tSD.pos = new Vector3(tSD.x*multiplier.x, tSD.y*multiplier.y, 0);

            // Слои сортировки
            tSD.layerID = int.Parse(slotsX[i].att("layer"));
            tSD.layerName = tSD.layerID.ToString();

            // Прочитать дополнительные атрибуты, опираясь на тип слота
            switch (tSD.type) {
                case "slot":
                    break; // Игнорировать слоты с типом "slot"
                
                case "drawpile":
                    tSD.stagger.x = float.Parse(slotsX[i].att("xstagger"));
                    drawPile = tSD;
                    break;

                case "discardpile":
                    discardPile = tSD;
                    break;

                case "target":
                    target = tSD;
                    break;

                case "hand":
                    tSD.player = int.Parse(slotsX[i].att("player"));
                    tSD.rot = float.Parse(slotsX[i].att("rot"));
                    slotDefs.Add (tSD);
                    break;
            }
        }
    }
}
