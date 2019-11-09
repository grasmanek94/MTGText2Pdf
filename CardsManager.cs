using System.Linq;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;

namespace MTGText2Pdf
{
    [Serializable]
    public class CardsManager
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<Card, bool>> cards;
        private ConcurrentDictionary<string, ConcurrentDictionary<Token, bool>> tokens;

        public CardsManager()
        {
            cards = new ConcurrentDictionary<string, ConcurrentDictionary<Card, bool>>();
            tokens = new ConcurrentDictionary<string, ConcurrentDictionary<Token, bool>>();
        }

        public bool Add(Card card)
        {
            ConcurrentDictionary<Card, bool> local_cards = new ConcurrentDictionary<Card, bool>();
            local_cards = cards.GetOrAdd(card.name, local_cards);
            return local_cards.TryAdd(card, true);
        }

        public bool Add(Token token)
        {
            ConcurrentDictionary<Token, bool> local_tokens = new ConcurrentDictionary<Token, bool>();
            local_tokens = tokens.GetOrAdd(token.name, local_tokens);
            return local_tokens.TryAdd(token, true);
        }

        public bool ContainsCard(string name)
        {
            return cards.ContainsKey(name);
        }

        public bool ContainsToken(string name)
        {
            return tokens.ContainsKey(name);
        }

        public Card GetNewestCard(string name, List<Card> banned = null)
        {
            ConcurrentDictionary<Card, bool> local_cards;
            if (!cards.TryGetValue(name, out local_cards))
            {
                return null;
            }

            Card ret_card = null;

            foreach (Card card in local_cards.Keys)
            {
                if (banned == null || !banned.Contains(card))
                {
                    ret_card = card;
                    break;
                }
            }

            if (ret_card == null)
            {
                return null;
            }

            foreach (Card card in local_cards.Keys)
            { 
                if(ret_card.releaseDate < card.releaseDate && card.imageUrls.Length > 0 && (banned == null || !banned.Contains(card)))
                {
                    ret_card = card;
                }
            }           

            return ret_card;
        }

        public Token GetNewestToken(string name, List<Token> banned = null)
        {
            ConcurrentDictionary<Token, bool> local_tokens;
            if (!tokens.TryGetValue(name, out local_tokens))
            {
                return null;
            }

            Token ret_token = null;

            foreach (Token token in local_tokens.Keys)
            {
                if (banned == null || !banned.Contains(token))
                {
                    ret_token = token;
                    break;
                }
            }

            if(ret_token == null)
            {
                return null;
            }

            foreach (Token token in local_tokens.Keys)
            {
                if (ret_token.releaseDate < token.releaseDate && token.imageUrls.Length > 0 && (banned == null || !banned.Contains(token)))
                {
                    ret_token = token;
                }
            }

            return ret_token;
        }

        public ImageAble GetNewest(string name)
        {
            ImageAble newest = GetNewestCard(name);
            if(newest == null)
            {
                return GetNewestToken(name);
            }
            return newest;
        }

        public ImageAble ReNew(ImageAble current, List<ImageAble> banned)
        {
            if(current as Card != null)
            {
                List<Card> banned_cards = new List<Card>();

                if(banned != null)
                {
                    banned_cards = banned.ConvertAll(x => (Card)x);
                }

                if(!banned_cards.Contains(current))
                {
                    banned_cards.Add(current as Card);
                }

                return GetNewestCard(current.GetName(), banned_cards);
            }

            if(current as Token != null)
            {
                List<Token> banned_cards = new List<Token>();

                if (banned != null)
                {
                    banned_cards = banned.ConvertAll(x => (Token)x);
                }

                if (!banned_cards.Contains(current))
                {
                    banned_cards.Add(current as Token);
                }

                return GetNewestToken(current.GetName(), banned_cards);
            }

            return null;
        }
    }
}
