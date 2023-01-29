using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Перечисление определяет разные этапы в течение одного игрового хода
public enum TurnPhase 
{
    idle,
    pre,
    waiting,
    post,
    gameOver
} 

public class Bartok : MonoBehaviour
{
    static public Bartok S;
    static public Player CURRENT_PLAYER;

    [Header("Set in Inspector")]
    public TextAsset            deckXML;
    public TextAsset            layoutXML;
    public Vector3              layoutCenter = Vector3.zero;
    public float                handFanDegrees = 10f;
    public int                  numStartingCards = 7;
    public float                drawTimeStagger = 0.1f;

    [Header("Set Dynamically")]
    public Deck                 deck;
    public List<CardBartok>     drawPile;
    public List<CardBartok>     discardPile;
    public List<Player>         players;
    public CardBartok           targetCard;
    public TurnPhase            phase = TurnPhase.idle;

    private BartokLayout        layout;
    private Transform           layoutAnchor;

    void Awake() 
    {
        S = this;
    }

    void Start()
    {
        deck = GetComponent<Deck>();    // Получить компонент Deck
        deck.InitDeck(deckXML.text);    // Передать ему DeckXML
        Deck.Shuffle(ref deck.cards);   // Перетасовать колоду

        layout = GetComponent<BartokLayout>();  // Получить ссылку на компонент Layout
        layout.ReadLayout(layoutXML.text);      // Передать ему LayoutXML

        drawPile = UpgradeCardsList(deck.cards);
        LayoutGame();     
    }

    // Метод приводит все карты в списке List<Card> к типу CardBartok
    List<CardBartok> UpgradeCardsList(List<Card> lCD) 
    {
        List<CardBartok> lCB = new List<CardBartok>();
        foreach(Card tCD in lCD) {
            lCB.Add(tCD as CardBartok);
        } return(lCB);
    }

    // Позиционирует все карты в drawpile
    public void ArrangeDrawPile() 
    {
        CardBartok tCB;
        for (int i=0; i<drawPile.Count; i++) {
            tCB = drawPile[i];
            tCB.transform.SetParent(layoutAnchor);
            tCB.transform.localPosition = layout.drawPile.pos;
            // Угол поворота начинается с нуля
            tCB.faceUp = false;
            tCB.SetSortingLayerName(layout.drawPile.layerName);
            tCB.SetSortOrder(-i*4); // Упорядочить от первых к последним
            tCB.state = CBState.drawpile;
        }
    }

    // Выполняет первоначальную раздачу карт в игре
    void LayoutGame() 
    {
        // СОздать пустой GameObject - точку привязки для раскладки
        if (layoutAnchor == null) {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        // Позиционировать свободные карты
        ArrangeDrawPile();

        // Настроить игроков
        Player pl;
        players = new List<Player>();
        foreach (SlotDef tSD in layout.slotDefs) {
            pl = new Player();
            pl.handSlotDef = tSD;
            players.Add(pl);
            pl.playerNum = tSD.player;
        }
        players[0].type = PlayerType.human; // 0-й игрок - человек

        CardBartok tCB;
        // Раздать игрокам по 7 карт
        for (int i=0; i<numStartingCards; i++) {
            for (int j=0; j<4; j++) {
                tCB = Draw(); // Снять карту
                // Немного отложить начало перемещения карты.
                tCB.timeStart = Time.time + drawTimeStagger * (i * 4 + j);
                players[(j + 1) % 4].AddCard(tCB);
            }
        }

        Invoke("DrawFirstTarget", drawTimeStagger * (numStartingCards * 4 + 4));
    }

    public void DrawFirstTarget()
    {
        // Перевернуть первую целевую карту лицевой стороной вверх
        CardBartok tCB = MoveToTarget(Draw());
        // Вызвать метод CBCallback, когда карта закончит перемещение
        tCB.reportFinishTo = this.gameObject;
    }

    // Этот обратный вызов используется последней розданной картой в начале игры
    public void CBCallBack(CardBartok cb)
    {
        Utils.tr("Bartok:CBCallBack()", cb.name);
        StartGame();
    }

    public void StartGame()
    {
        // Право первого хода принадлежит игроку слева от человека
        PassTurn(1);
    }

    public void PassTurn(int num =-1)
    {
        // Если порядковый номер игрока не указан, выбрать следующего по кругу
        if (num == -1) {
            int ndx = players.IndexOf(CURRENT_PLAYER);
            num = (ndx + 1) % 4;
        }
        
        int lastPlayerNum = -1;
        
        if (CURRENT_PLAYER != null) {
            lastPlayerNum = CURRENT_PLAYER.playerNum;
            // Проверить завершение игры и необходимость перетасовать стопку сброшенных карт
            if (CheckGameOver()) {return;}
        }
        CURRENT_PLAYER = players[num];
        phase = TurnPhase.pre;
        CURRENT_PLAYER.TakeTurn();
        
        // Сообщить о передаче кода
        Utils.tr("Bartok:PassTurn()", "Old: "+lastPlayerNum, "New: "+CURRENT_PLAYER.playerNum);
    }

    public bool CheckGameOver() 
    {
        // Проверить нужно ли перетасовать стопку сброшенных карт и перенести ее в стопку свободных карт
        if (drawPile.Count == 0) {
            List<Card> cards = new List<Card>();
            foreach (CardBartok cb in discardPile) {cards.Add(cb);}
            discardPile.Clear();
            Deck.Shuffle(ref cards);
            drawPile = UpgradeCardsList(cards);
            ArrangeDrawPile();
        }
        // Проверить победу текущего игрока
        if (CURRENT_PLAYER.hand.Count == 0) {
            phase = TurnPhase.gameOver;
            Invoke("RestartGame", 1);
            return(true);
        }
        return(false);
    }

    public void RestartGame()
    {
        CURRENT_PLAYER = null;
        SceneManager.LoadScene("__Bartok_Scene_0");
    }

    // Проверяет возможность сыграть выбранной картой
    public bool ValidPlay(CardBartok cb)
    {
        // Картой можно сыграть, если она имеет достоинство как целевая карта
        if (cb.rank == targetCard.rank) return(true);

        // Картой можно сыграть, если ее масть совпадает с мастью целевой карты
        if (cb.suit == targetCard.suit) return(true);
        return(false);
    }

    // Делает указанную карту целевой
    public CardBartok MoveToTarget(CardBartok tCB) 
    {
        tCB.timeStart = 0;
        tCB.MoveTo(layout.target.pos + Vector3.back);
        tCB.state = CBState.toTarget;
        tCB.faceUp = true;

        tCB.SetSortingLayerName("10");
        tCB.eventualSortLayer = layout.target.layerName;
        if (targetCard != null) {
            MoveToDiscard(targetCard);
        }

        targetCard = tCB;
        return(tCB);
    }

    public CardBartok MoveToDiscard(CardBartok tCB) {
        tCB.state = CBState.discard;
        discardPile.Add (tCB);
        tCB.SetSortingLayerName(layout.discardPile.layerName);
        tCB.SetSortOrder(discardPile.Count * 4);
        tCB.transform.localPosition = layout.discardPile.pos + Vector3.back/2;
        return(tCB);
    }

    // Функция Draw снимает верхнюю карту со стопки свободных карт и возвращает ее
    public CardBartok Draw()
    {
        CardBartok cd = drawPile[0];    // Извлечь 0-ю карту
        // Если список опустел, нужно перетасовать сброшенные карты и переложить их
        if (drawPile.Count == 0) {
            int ndx;
            while (discardPile.Count > 0) {
                // Вынуть случайную карту из стопки сброшенных карт
                ndx = Random.Range(0, discardPile.Count);
                drawPile.Add(discardPile[ndx]);
                discardPile.RemoveAt(ndx);
            }
            ArrangeDrawPile();
            // Показать перемещение карт в стопку свободных карт
            float t = Time.time;
            foreach (CardBartok tCB in drawPile) {
                tCB.transform.localPosition = layout.discardPile.pos;
                tCB.callbackPlayer = null;
                tCB.MoveTo(layout.drawPile.pos);
                tCB.timeStart = t;
                t += 0.02f;
                tCB.state = CBState.toDrawpile;
                tCB.eventualSortLayer = "0";
            }
        }
        drawPile.RemoveAt(0);           // Удалить ее из списка drawpile
        return (cd);                    // и вернуть ее
    }

    public void CardClicked(CardBartok tCB)
    {
        if (CURRENT_PLAYER.type != PlayerType.human) return;
        if (phase == TurnPhase.waiting) return;

        switch (tCB.state) {
            case CBState.drawpile:
                // Взять верхнюю карту
                CardBartok cb = CURRENT_PLAYER.AddCard(Draw());
                cb.callbackPlayer = CURRENT_PLAYER;
                Utils.tr ("Bartok.CardClicked()", "Draw", cb.name);
                phase = TurnPhase.waiting;
                break;
            case CBState.hand:
                // Проверить допустимость выбранной карты
                if (ValidPlay(tCB)) {
                    CURRENT_PLAYER.RemoveCard(tCB);
                    MoveToTarget(tCB);
                    tCB.callbackPlayer = CURRENT_PLAYER;
                    Utils.tr ("Bartok:CardClicked()", "Play", tCB.name, targetCard.name+ " is target");
                    phase = TurnPhase.waiting;
                } else {
                    // Игнорировать выбор недопустимой карты, но сообщить о попытке игроку
                    Utils.tr ("Bartok.CardClicked()", "Attempted to Play", tCB.name, targetCard.name+ " is target");
                } break;
        }
    }
}
