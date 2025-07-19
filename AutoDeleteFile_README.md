# �Զ�ɾ���ļ�����ʹ��˵��

## ����
������ʵ���˻��ڴ��̿ռ��ص��Զ��ļ�ɾ�����ܡ�������ʣ��ռ�����趨��ֵʱ���Զ�ɾ������Ŀ¼�еľ��ļ���ֱ�����̿ռ�ָ�����ȫˮƽ��

## �����ص�
1. **�����֧��**��֧��ͬʱ��ض������������
2. **�������**��ͨ��INI�����ļ�����ɾ��Ŀ¼���ռ���ֵ�Ȳ���
3. **����ɾ��**�����ļ��޸�ʱ����������ɾ����ɵ��ļ�
4. **��ȫ����**���ﵽֹͣɾ����ֵ���Զ�ֹͣ����
5. **��ϸ��־**����¼��ϸ��ɾ�����̺ͽ��

## �����ļ���ʽ
�����ļ�·����`����Ŀ¼/AutoDeleteFile.ini`

```ini
# �Զ�ɾ���ļ�����
# ��ʽ��[����������]
# DeleteDirectories=Ŀ¼1,Ŀ¼2,Ŀ¼3
# StartDeleteSizeGB=��ʼɾ��ʱ�Ĵ���ʣ��ռ�(GB)
# StopDeleteSizeGB=ֹͣɾ��ʱ�Ĵ���ʣ��ռ�(GB)

[C:]
DeleteDirectories=C:\temp,C:\Windows\temp,C:\Users\Public\temp
StartDeleteSizeGB=5.0
StopDeleteSizeGB=10.0

[D:]
DeleteDirectories=D:\temp,D:\logs,D:\cache
StartDeleteSizeGB=10.0
StopDeleteSizeGB=20.0
```

## ���ò���˵��
- **[������]**��������������ʶ���� `[C:]`��`[D:]`
- **DeleteDirectories**��Ҫ�����Ŀ¼�б��ö��ŷָ���֧����Ŀ¼�ݹ�ɾ��
- **StartDeleteSizeGB**����ʼɾ���Ĵ���ʣ��ռ���ֵ��GB��
- **StopDeleteSizeGB**��ֹͣɾ���Ĵ���ʣ��ռ���ֵ��GB��

## ʹ�÷���

### 1. �����Զ�ɾ������
```csharp
// �ڳ�������ʱ����
AutoDeleteFile.Start();
```

### 2. ��������
```csharp
// ��ȡ��ǰ����
var configs = AutoDeleteFileManager.GetCurrentConfigs();

// ��ӻ��������
AutoDeleteFileManager.AddOrUpdateConfig("C:", 
    new List<string> { @"C:\temp", @"C:\logs" },
    5.0, 10.0);

// ɾ������
AutoDeleteFileManager.RemoveConfig("C:");
```

### 3. ���Թ���
```csharp
// ��������
AutoDeleteFileTest.TestConfiguration();

// ��Ӳ�������
AutoDeleteFileTest.AddTestConfiguration();

// ģ���������
AutoDeleteFileTest.SimulateCleanup();
```

## ��������
1. **���ö�ȡ**����������ʱ��ȡINI�����ļ�
2. **�ռ���**��ÿ60����һ�δ���ʣ��ռ�
3. **��������**����ʣ��ռ�ܿ�ʼɾ����ֵʱ����ʼ����
4. **�ļ�ɨ��**��ɨ������Ŀ¼�е������ļ���������Ŀ¼��
5. **����ɾ��**�����ļ��޸�ʱ����������ɾ����ɵ��ļ�
6. **���ֹͣ**����ʣ��ռ��ֹͣɾ����ֵʱ��ֹͣ����

## ��ȫע������
1. **��������ɾ��Ŀ¼**����������ϵͳ�ؼ�Ŀ¼
2. **����������ֵ**��ȷ��ֹͣɾ����ֵ���ڿ�ʼɾ����ֵ
3. **���ڼ����־**�����ɾ�������Ƿ�����
4. **������Ҫ�ļ�**��ɾ��ǰȷ����Ҫ�ļ��ѱ���

## ��־��¼
������¼������Ϣ��
- �����ļ���ȡ���
- ���̿ռ�����
- �ļ�ɾ������
- ������쳣��Ϣ

��־�ļ�λ�ã�`����Ŀ¼/Logs/`

## ʾ������
```ini
# ������������
[C:]
DeleteDirectories=C:\temp,C:\Windows\temp,C:\Users\%USERNAME%\AppData\Local\Temp
StartDeleteSizeGB=2.0
StopDeleteSizeGB=5.0

# ������������
[D:]
DeleteDirectories=D:\logs,D:\temp,D:\cache
StartDeleteSizeGB=20.0
StopDeleteSizeGB=50.0
```

## �����ų�
1. **�����ļ�������**��������Զ�����Ĭ�������ļ�
2. **Ŀ¼������**��������־�м�¼���棬��Ӱ������Ŀ¼������
3. **�ļ�ɾ��ʧ��**�����¼������־���������������ļ�
4. **���̲�����**���������ô��̵���������

## API�ο�
- `AutoDeleteFile.Start()`�������Զ�ɾ������
- `AutoDeleteFile.Stop()`��ֹͣ�Զ�ɾ������
- `AutoDeleteFileManager.GetCurrentConfigs()`����ȡ��ǰ����
- `AutoDeleteFileManager.SaveConfigs()`����������
- `AutoDeleteFileManager.GetDriveInfos()`����ȡ������Ϣ