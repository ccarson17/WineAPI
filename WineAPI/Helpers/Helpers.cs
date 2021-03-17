using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI
{
    public static class Extensions
    {
        public static TResult MergeInto<TResult>(this IMapper mapper, object item1, object item2)
        {
            return mapper.Map(item2, mapper.Map<TResult>(item1));
        }

        public static TResult MergeInto<TResult>(this IMapper mapper, params object[] objects)
        {
            var res = mapper.Map<TResult>(objects.First());
            return objects.Skip(1).Aggregate(res, (r, obj) => mapper.Map(obj, r));
        }
    }
}
