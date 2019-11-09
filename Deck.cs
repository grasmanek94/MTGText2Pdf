using System;
using System.Collections.Generic;
using System.Xml;

namespace MTGText2Pdf
{
    public class Deck
    {
        public Dictionary<ImageAble, int> cards;

        public Deck()
        {
            cards = new Dictionary<ImageAble, int>();
        }

        public static Deck Parse(CardsManager manager, string filename)
        {
            Deck deck = new Deck();

            XmlReader xmlReader = XmlReader.Create(filename);
            while (xmlReader.Read())
            {
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "card"))
                {
                    if (xmlReader.HasAttributes)
                    {
                        int amount = int.Parse(xmlReader.GetAttribute("number"));
                        string name = xmlReader.GetAttribute("name");

                        ImageAble image_able = manager.GetNewest(name);
                        if(image_able == null)
                        {
                            Console.WriteLine("[W] WARNING: Cannot find card \"" + name + "\"");
                            continue;
                        }

                        if(amount < 1)
                        {
                            Console.WriteLine("[W] WARNING: Amount = 0, card \"" + name + "\"");
                            continue;
                        }

                        deck.Add(image_able, amount);
                    }
                }
            }

            return deck;
        }

        public void Add(ImageAble image_able, int amount)
        {
            if(image_able == null)
            {
                return;
            }

            if (cards.ContainsKey(image_able))
            {
                cards[image_able] += amount;
            }
            else
            {
                cards.Add(image_able, amount);
            }
        }

        public void Add(Deck deck)
        {
            if(deck == null)
            {
                return;
            }

            foreach(var imageable in deck.cards)
            {
                Add(imageable.Key, imageable.Value);
            }
        }
    }
}
