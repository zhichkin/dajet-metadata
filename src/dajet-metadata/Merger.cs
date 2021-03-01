using DaJet.Metadata.Model;
using System.Collections.Generic;

namespace DaJet.Metadata
{
    public sealed class Merger
    {
        public void Merge(List<string> target_list, List<string> source_list, out List<string> delete_list, out List<string> insert_list)
        {
            delete_list = new List<string>();
            insert_list = new List<string>();

            int target_count = target_list.Count;
            int source_count = source_list.Count;
            int target_index = 0;
            int source_index = 0;
            int compareResult;

            if (target_count == 0 && source_count == 0) return;

            //target_list.Sort();
            //source_list.Sort();

            while (target_index < target_count) // target список "ведущий"
            {
                if (source_index < source_count) // в source списке ещё есть элементы
                {
                    compareResult = target_list[target_index].CompareTo(source_list[source_index]);
                    if (compareResult < 0) // target меньше source
                    {
                        // Элемент target больше не может встретиться в списке source.
                        delete_list.Add(target_list[target_index]);
                        // Добавлять элемент source в список insert мы пока не можем,
                        // так как элемент source возможно есть ниже в списке target.
                        target_index++; // Берём следующий элемент target
                    }
                    else if (compareResult == 0) // target равен source
                    {
                        target_index++; // Берём следующий элемент target
                        source_index++; // Берём следующий элемент source
                    }
                    else // target больше source
                    {
                        // Добавлять элемент target в список delete мы пока не можем,
                        // так как элемент target возможно есть ниже в списке source.
                        // Элемент source больше не может встретиться в списке target.
                        insert_list.Add(source_list[source_index]);
                        source_index++; // Берём следующий элемент source
                    }
                }
                else // достигли конца source списка
                {
                    delete_list.Add(target_list[target_index]);
                    target_index++; // Берём следующий элемент target
                }
            }
            while (source_index < source_count) // target список оказался короче source списка
            {
                // Добавляем все оставшиеся элементы source
                insert_list.Add(source_list[source_index]);
                source_index++; // Берём следующий элемент source
            }
        }
        public List<string> PrepareForMerge(MetaObject metaObject)
        {
            List<string> fields = new List<string>(metaObject.Properties.Count);
            for (int i = 0; i < metaObject.Properties.Count; i++)
            {
                fields.Add(metaObject.Properties[i].Field);
            }
            fields.Sort();
            return fields;
        }
        public List<string> PrepareForMerge(List<SqlFieldInfo> sqlFields)
        {
            List<string> fields = new List<string>(sqlFields.Count);
            for (int i = 0; i < sqlFields.Count; i++)
            {
                fields.Add(sqlFields[i].COLUMN_NAME);
            }
            return fields;
        }
    }
}