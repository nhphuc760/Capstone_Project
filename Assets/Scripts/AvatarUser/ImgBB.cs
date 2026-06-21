using System;

[Serializable] 

public class ImgBBRespone 
{
    public ImgBBData data;
    public bool success;
}


[Serializable] 
public class ImgBBData 
{
    public string url;
}