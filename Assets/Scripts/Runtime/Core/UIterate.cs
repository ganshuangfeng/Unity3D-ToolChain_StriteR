using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Procedural.Hexagon.Geometry;

public static class UIterate
{
    #region Array
    static class ArrayStorage<T>
    {
        public static T[] m_Array=new T[0];
        public static void CheckLength(int length)
        {
            if (m_Array.Length != length)
                m_Array = new T[length];
        }
    }
    public static T[] Iterate<T>(this IIterate<T> helper)
    {
        ArrayStorage<T>.CheckLength(helper.Length);
        for (int i = 0; i < helper.Length; i++)
            ArrayStorage<T>.m_Array[ i] = helper[i];
        return ArrayStorage<T>.m_Array;
    }
    public static Y[] Iterate<T,Y>(this IIterate<T> helper,Func<T,Y> _convert)
    {
        ArrayStorage<Y>.CheckLength(helper.Length);
        for(int i=0;i<helper.Length;i++)
            ArrayStorage<Y>.m_Array[i] = _convert(helper[i]);
        return ArrayStorage<Y>.m_Array;
    }
    #endregion

    public static void Traversal<T>(this IIterate<T> _src, Action<T> _OnEach)
    {
        int length = _src.Length;
        for(int i=0;i<length;i++)
            _OnEach(_src[i]);
    }
    
    public static void Traversal<T>(this IIterate<T> _src, Action<int,T> _OnEach)
    {
        int length = _src.Length;
        for(int i=0;i<length;i++)
            _OnEach(i,_src[i]);
    }

    public static T Find<T>(this IIterate<T> _src,Predicate<T> _predicate)
    {
        int length = _src.Length;
        for(int i=0;i<length;i++)
        {
            var element = _src[i];
            if (_predicate(_src[i]))
                return element;
        }
        return default;
    }
    
    public static int FindIndex<T>(this IIterate<T> _src,Predicate<T> _predicate)
    {
        int length = _src.Length;
        for(int i=0;i<length;i++)
            if (_predicate(_src[i]))
                return i;
        return -1;
    }

    public static bool Contains<T>(this IIterate<T> _src, T _element) 
    {
        int length = _src.Length;
        for(int i=0;i<length;i++)
            if (_src[i].Equals(_element))
                return true;
        return false;
    }
    public static bool Any<T>(this IIterate<T> _src, Predicate<T> _validate)
    {
        int length = _src.Length;
        for(int i=0;i<length;i++)
            if (_validate(_src[i]))
                return true;
        return false;
    }
    
    public static bool All<T>(this IIterate<T> _src, Predicate<T> _validate)
    {
        int length = _src.Length;
        for(int i=0;i<length;i++)
            if (!_validate(_src[i]))
                return false;
        return true;
    }
    
    public static void AddRange<T>(this IList<T> _src,IIterate<T> _iterate)
    {
        int length = _iterate.Length;
        for(int i=0;i<length;i++)
            _src.Add(_iterate[i]);
    }

    public static T[] ToArray<T>(this IIterate<T> _iterate)
    {
        int length = _iterate.Length;
        T[] array = new T[length];
        for(int i=0;i<length;i++)
            array[i]=_iterate[i];
        return array;
    }
}