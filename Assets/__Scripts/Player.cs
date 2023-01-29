using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Подключает механизм запросов LINQ

// Игрок может быть человеком или ИИ
public enum PlayerType {human, ai}

[System.Serializable]
public class Player 
{
    public PlayerType           type = PlayerType.ai;
    public int                  playerNum;
    public SlotDef              handSlotDef;
    public List<CardBartok>     hand; // Карты в руках игрока

    // Добавляет карту в руки
    public CardBartok AddCard(CardBartok eCB)
    {
        if (hand == null) hand = new List<CardBartok>();
        hand.Add(eCB);
        // Если это человек, отсортировать карты по достоинству с помощью LINQ
        if (type == PlayerType.human) {
            CardBartok[] cards = hand.ToArray();
            cards = cards.OrderBy(cd => cd.rank).ToArray(); // Это вызов LINQ
            hand = new List<CardBartok>(cards);
            // LINQ выполняет операции довольно медленно (затрачивая несколько миллисекунд),
            // но так как мы делаем это один раз за раунд, это не проблема.
        }

        eCB.SetSortingLayerName("10"); // Перенести перемещаемую карту в верхний слой
        eCB.eventualSortLayer = handSlotDef.layerName;

        FanHand();
        return(eCB);
    }

    // Удаляет карту из рук
    public CardBartok RemoveCard(CardBartok cb)
    {
        // Если список hand пуст или не содержит карты cb, вернуть null
        if (hand == null || !hand.Contains(cb)) return null;
        hand.Remove(cb);
        FanHand();
        return(cb);
    }

    public void FanHand() 
    {
        // startRot - угол поворота первой карты относительно оси Z
        float startRot = 0;
        startRot = handSlotDef.rot;
        if (hand.Count > 1) {startRot += Bartok.S.handFanDegrees * (hand.Count-1) / 2;}
        // Переместить все карты в новые позиции
        Vector3 pos;
        float rot;
        Quaternion rotQ;
        for (int i=0; i<hand.Count; i++) {
            rot = startRot - Bartok.S.handFanDegrees * i;
            rotQ = Quaternion.Euler(0, 0, rot);
            pos = Vector3.up * CardBartok.CARD_HEIGHT / 2f;
            pos = rotQ * pos;

            // Прибавить координаты позиции руки игрока (внизу в центре веера карт)
            pos += handSlotDef.pos;
            pos.z = -0.5f * i;

            // Если это не начальная раздача, начать перемещение карты немедленно
            if (Bartok.S.phase != TurnPhase.idle) {hand[i].timeStart = 0;}

            // Установить локальную позицию и поворот i-й карты в руках
            // Сообщить карте, что она должна начать интерполяцию
            hand[i].MoveTo(pos, rotQ);
            // Закончив перемещение, карта запишет в поле state значение CBState.hand 
            hand[i].state = CBState.toHand;

            /*hand[i].transform.localPosition = pos;
            hand[i].transform.rotation = rotQ;
            hand[i].state = CBState.hand;*/

            hand[i].faceUp = (type == PlayerType.human);

            // Установить SortOrder карт, чтобы обеспечить правильное перекрытие
            hand[i].eventualSortOrder = i*4; 
            //hand[i].SetSortOrder(i*4);
        }
    }

    // Функция реализует ИИ для игроков, управляемых компьютером
    public void TakeTurn()
    {
        Utils.tr ("Player.TakeTurn");
        if (type == PlayerType.human) return;
        Bartok.S.phase = TurnPhase.waiting;
        CardBartok cb;
        // Если этим игроком управляет компьютер, нужно выбрать карту для хода
        // Найти допустимые ходы
        List<CardBartok> validCards = new List<CardBartok>();
        foreach (CardBartok tCB in hand) {
            if (Bartok.S.ValidPlay(tCB)) {validCards.Add(tCB);}
        }
        // Если допустимых ходов нет
        if (validCards.Count == 0) {
            cb = AddCard(Bartok.S.Draw()); // взять карту
            cb.callbackPlayer = this;
            return;
        }

        // Выбрать одну из карт, которой можно сыграть
        cb = validCards[Random.Range(0, validCards.Count)];
        RemoveCard(cb);
        Bartok.S.MoveToTarget(cb);
        cb.callbackPlayer = this;
    }

    public void CBCallBack(CardBartok tCB)
    {
        Utils.tr ("Player.CBCallback()", tCB.name, "Player " + playerNum);
        // Карта завершила перемещение, передать право хода
        Bartok.S.PassTurn();
    }
}