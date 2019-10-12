using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
    [Serializable]
    public class ObjectPool
    {
        public bool IsExpandable = true;
        public CustomBehaviour m_template;
        public int m_cnt = 0;
        private List<CustomBehaviour> m_pool = new List<CustomBehaviour>();
        private List<bool> m_used = new List<bool>();

        public CustomBehaviour GetAt(int i)
        {
            if (i < 0 || i > m_cnt)
                return null;

            return m_pool[i];
        }

        public int IndexOf(CustomBehaviour element)
        {
            if (element == null || m_pool == null)
                return -1;

            return m_pool.IndexOf(element);
        }

        public void Init()
        {
            AddInstances(m_cnt);
        }

        public void Expand(int i_cnt)
        {
            m_cnt += i_cnt;

            int changeAmount = i_cnt;

            if (m_cnt < 0)
            {
                changeAmount = Math.Abs(i_cnt) + m_cnt;
                m_cnt = 0;
            }

            if (i_cnt > 0)
                AddInstances(changeAmount);
            else
                RemoveInstances(changeAmount);
        }

        private void AddInstances(int i_cnt)
        {
            for (int i = 0; i < i_cnt; ++i)
            {
                var pooledObj = Instantiate(m_template, PoolManager.instance.gameObject.transform);
                pooledObj.gameObject.SetActive(false);
                m_pool.Add(pooledObj);
                m_used.Add(false);
            }

            UIManager.instance.RefreshPoolCount();
        }

        private void RemoveInstances(int i_cnt)
        {
            int itemCnt = Math.Abs(i_cnt);

            for (int i = 0; i < itemCnt; ++i)
            {
                m_pool[m_pool.Count - 1].StopAllCoroutines();
                m_pool[m_pool.Count - 1].gameObject.SetActive(false);
                GameObject.Destroy(m_pool[m_pool.Count - 1].gameObject);
                m_used.RemoveAt(m_pool.Count - 1);
                m_pool.RemoveAt(m_pool.Count - 1);
            }

            UIManager.instance.RefreshPoolCount();
        }

        public CustomBehaviour SpawnObject(Vector3 i_position, Quaternion i_rotation, Transform parent = null)
        {
            int index = m_used.IndexOf(false);
            if (index >= 0 && index < m_pool.Count)
            {
                m_used[index] = true;
                var objToReturn = m_pool[index];

                if (parent != null)
                    objToReturn.transform.SetParent(parent);

                objToReturn.Restart();
                objToReturn.transform.position = i_position;
                objToReturn.transform.rotation = i_rotation;
                objToReturn.gameObject.SetActive(true);
                return objToReturn;
            }
            else
            {
                if (IsExpandable)
                {
                    Expand(1);
                    return SpawnObject(i_position, i_rotation, parent);
                }
                else
                {
                    //Debug.LogWarning("Wrong index in object pool!");
                }
            }

            return null;
        }

        public void ReturnToPool(CustomBehaviour i_obj)
        {
            i_obj.StopAllCoroutines();

            if (i_obj.transform.parent != PoolManager.instance.gameObject.transform)
                i_obj.transform.SetParent(PoolManager.instance.gameObject.transform);

            i_obj.gameObject.SetActive(false);
            int index = m_pool.IndexOf(i_obj);
            if (index >= 0 && index < m_used.Count)
            {
                m_used[index] = false;
            }
            else
            {
                Debug.LogWarning("Returning to the wrong pool!");
            }
        }
    }

    public ObjectPool MobPool;
    public ObjectPool SpearPool;

    public void Start()
    {
        MobPool.Init();
        SpearPool.Init();
    }
}