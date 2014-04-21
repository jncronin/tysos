using System;

namespace testca
{
    class Node<T>
    {
        public T obj;
        public Node<T> prev, next;
    }    
    
    class List<T>
    {
        Node<T> first = null;
        Node<T> last = null;
        int count = 0;

        public void Add(T obj)
        {
            Node<T> new_item = new Node<T>();
            new_item.obj = obj;
            new_item.next = null;

            if (last != null)
            {
                last.next = new_item;
                new_item.prev = last;
                last = new_item;
            }
            else
            {
                last = first = new_item;
                new_item.prev = null;
            }

            count++;
        }

        public int Count()
        { return count; }
    }
}
