using SpawnDev.BlazorJS.JSObjects;

namespace Tolerador.WebSiteExtensions
{
    public class WatchNode
    {
        public delegate Element? QuerySelectorDelegate(Document document);
        public string Name { get; set; }
        QuerySelectorDelegate? SelectorFn { get; set; }
        public string Selector { get; set; }
        public bool Found { get; set; }
        public bool CheckVisibility { get; set; }
        public CheckVisibilityOptions? CheckVisibilityOptions { get; set; }
        public Action<WatchNode>? OnFound { get; set; }
        public Action<WatchNode>? OnLost { get; set; }
        public Element? Query(Document? document) => Query<Element>(document);
        public TElement? Query<TElement>(Document? document) where TElement : Element
        {
            if (document == null) return null;
            if (SelectorFn != null)
            {
                var el = SelectorFn(document);
                return el == null ? null : el.JSRefMove<TElement>();
            }
            TElement? ret = null;
            using var nodeList = document?.QuerySelectorAll<TElement>(Selector);
            if (nodeList != null && nodeList.Length > 0)
            {
                if (CheckVisibility)
                {
                    //var elements = nodeList.ToArray<TElement>();
                    foreach (TElement element in nodeList)
                    {
                        if (ret == null && element.CheckVisibility(CheckVisibilityOptions))
                        {
                            ret = element;
                        }
                        else
                        {
                            element.Dispose();
                        }
                    }
                }
                else
                {
                    ret = nodeList.Item(0);
                }
            }
            return ret;
        }
        List<TElement> QueryAll<TElement>(Document? document) where TElement : Element
        {
            var ret = new List<TElement>();
            using var nodeList = document?.QuerySelectorAll<TElement>(Selector);
            if (nodeList != null && nodeList.Length > 0)
            {
                if (CheckVisibility)
                {
                    foreach (TElement element in nodeList)
                    {
                        if (element.CheckVisibility(CheckVisibilityOptions))
                        {
                            ret.Add(element);
                        }
                        else
                        {
                            element.Dispose();
                        }
                    }
                }
                else
                {
                    ret.AddRange(nodeList);
                }
            }
            return ret;
        }

        public WatchNode(string name, QuerySelectorDelegate selectorFn, bool checkVisibility = false, Action<WatchNode>? onFound = null, Action<WatchNode>? onLost = null)
        {
            Name = name;
            SelectorFn = selectorFn;
            Selector = "";
            OnFound = onFound;
            OnLost = onLost;
            CheckVisibility = checkVisibility;
        }
        public WatchNode(string name, string selector, bool checkVisibility = false, Action<WatchNode>? onFound = null, Action<WatchNode>? onLost = null)
        {
            Name = name;
            Selector = selector;
            OnFound = onFound;
            OnLost = onLost;
            CheckVisibility = checkVisibility;
        }
        public WatchNode(string name, string selector, CheckVisibilityOptions checkVisibilityOptions, Action<WatchNode>? onFound = null, Action<WatchNode>? onLost = null)
        {
            Name = name;
            Selector = selector;
            OnFound = onFound;
            OnLost = onLost;
            CheckVisibility = checkVisibilityOptions != null;
            CheckVisibilityOptions = checkVisibilityOptions;
        }
    }
}
