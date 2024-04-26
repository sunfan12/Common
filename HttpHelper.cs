using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;


namespace InternelSDK
{
    public class HttpHelper : Singleton<HttpHelper>
    {
        Action<string> httpLog;
        Action<string> httpLogWarning;
        Action<string> httpLogError;
        public async Task ResumeDownload(string url, string localPath, bool isOverWrite = true, Action callback = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            FileInfo localFile = new FileInfo(localPath);
            long startPosition = 0;
            httpLog?.Invoke("Start");
            if (localFile.Exists)
            {
                // ��������ļ��Ѿ����ڣ����ȡ�Ѿ����ص����ݳ���
                httpLog?.Invoke("Exists");
                if (!isOverWrite)
                {
                    startPosition = localFile.Length;
                    httpLog?.Invoke("startPosition:" + (int)startPosition);
                    request.AddRange((int)startPosition);
                }
                // ����http����ͷ�е�Range���ԣ��Ա������֪����Ҫ������Щ����
            }
            // �������󣬻�ȡ��������Ӧ
            HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
            Stream responseStream = response.GetResponseStream();
            // ��������ļ������ڣ����½�һ���ļ�
            if (!localFile.Exists)
            {
                localFile.Create().Close();
            }
            byte[] buffer = new byte[2048];
            int len;
            len = await responseStream.ReadAsync(buffer, 0, buffer.Length);
            //Debug.Log("length:" + responseStream.Length);
            // ʹ���ļ������������������ݶ�д
            int i = 0;
            using (FileStream localFileStream = localFile.Open(FileMode.OpenOrCreate))
            {
                localFileStream.Seek(startPosition, SeekOrigin.Begin); // ���ļ�ָ��ָ��Ӧ�ÿ�ʼ���ص�λ��
                httpLog?.Invoke("Start");
                while (len != 0)
                {
                    httpLog?.Invoke("write");
                    // д�뱾���ļ�
                    i++;
                    await localFileStream.WriteAsync(buffer, 0, len);
                    localFileStream.Flush();
                    len = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                }
            }
            callback?.Invoke();
        }


        public async Task<bool> ResumeDownload(string url, MemoryStream ms, Action callback = null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                long startPosition = 0;
                httpLog?.Invoke("Start");


                // �������󣬻�ȡ��������Ӧ
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                Stream responseStream = response.GetResponseStream();
                // ��������ļ������ڣ����½�һ���ļ�

                byte[] buffer = new byte[2048];
                int len;
                len = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                //Debug.Log("length:" + responseStream.Length);
                // ʹ���ļ������������������ݶ�д
                int i = 0;

                ms.Seek(startPosition, SeekOrigin.Begin); // ���ļ�ָ��ָ��Ӧ�ÿ�ʼ���ص�λ��
                while (len != 0)
                {
                    // д�뱾���ļ�
                    i++;
                    await ms.WriteAsync(buffer, 0, len);
                    ms.Flush();
                    len = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                }
                callback?.Invoke();
                return true;
            }
            catch (Exception e){
                httpLogError?.Invoke(e.ToString());
                return false;
            }   
        }

        public async void LoadImage(string url,Vector2Int size, Action<Sprite> callbacks = null)
        {
            if (string.IsNullOrEmpty(url))
                return;
            MemoryStream ms = new MemoryStream();
            await HttpHelper.Instance.ResumeDownload(url,
               ms);
            Texture2D tx = new Texture2D(size.x, size.y);
            tx.LoadImage(ms.ToArray());
            Sprite sp = Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), Vector2.zero);
            callbacks.Invoke(sp);
        }

        public async Task<bool> CheckFile(string hash, string localPath)
        {
            string code = await FileTool.Instance.ProcessReadAsync(localPath + ".mate");
            if (code.Trim().ToLower() == hash.Trim().ToLower())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private const string PlayStoreUrl = "https://play.google.com/store/apps/details?id={0}";//�ȸ��̵�
        private const string AppStoreUrl = "https://itunes.apple.com/app/apple-store/id{0}";//ƻ��Ӧ���̵�  
    }
}