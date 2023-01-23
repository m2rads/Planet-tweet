using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Media
{
    public string media_url_https { get; set; }
    public string type { get; set; }
}

public class Entities
{
    public List<Media> media { get; set; }
}

public class User
{
    public string name { get; set; }
    public string profile_image_url_https { get; set; }
}

public class BoundingBox
{
    public string type { get; set; }
    public List<List<List<double>>> coordinates { get; set; }
}

public class Place
{
    public BoundingBox bounding_box { get; set; }
}

public class Status
{
    public string created_at { get; set; }
    public long id { get; set; }
    public string text { get; set; }
    public Entities entities { get; set; }
    public User user { get; set; }
    public Place place { get; set; }
    public bool possibly_sensitive { get; set; }
    public string lang { get; set; }
}

public class SearchMetadata
{
    public double completed_in { get; set; }
    public int count { get; set; }
}

public class TweetResponseObject
{
    public List<Status> statuses { get; set; }
    public SearchMetadata search_metadata { get; set; }
}

