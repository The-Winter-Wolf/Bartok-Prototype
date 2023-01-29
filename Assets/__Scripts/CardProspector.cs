using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public enum eCardState // Перечисление, определяющее тип переменной
    {
        drawpile,   // Свободные карты
        tableau,    // Основная раскладка из 28 карт
        target,     // Целевая активная карта
        discard     // Сброшенные в стопку карты
    }

    public class CardProspector : Card // Расширяет класс Card
    {
        [Header("Set Dynamically: CardProspector")]
        public eCardState               state = eCardState.drawpile;
        // hiddenBy - список других карт, не позволяющих перевернуть эту лицом вверх
        public List<CardProspector>     hiddenBy = new List<CardProspector>();
        // layoutID определяет для этой карты ряд в раскладке
        public int                      layoutID;
        // Класс SlotDef хранит информацию из элемента <slot> в LayoutXML
        public SlotDef                  slotDef;

        // Определяет реакцию карт на щелчок мыши
        override public void OnMouseUpAsButton()
        {
            // Вызвать метод CardClicked объекта-одиночки Prospector
            Prospector.S.CardClicked(this);
            // А также версию этого метода в базовом классе (Card.cs)
            base.OnMouseUpAsButton(); 
        }
    }