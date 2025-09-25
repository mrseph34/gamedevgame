using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Combat/Combo Attack")]
public class ComboModule : AttackModule
{
    [Header("Hitbox Graphics")]
    public Sprite hitboxSprite;
    public Color hitboxColor = new Color(1, 0, 0, 0.5f);

    [Header("Sizes & Offsets")]
    public Vector2 hbHorizontalSize = new Vector2(5, 10);
    public Vector2 hbHorizontalOff = new Vector2(1, 0);
    public Vector2 hbUpSize = new Vector2(10, 5);
    public Vector2 hbUpOff = new Vector2(0f, 1.75f);
    public Vector2 hbDownSize = new Vector2(10, 5);
    public Vector2 hbDownOff = new Vector2(0f, -1.5f);
    public Vector2 hbDiagSize = new Vector2(5, 10);
    public Vector2 hbDiagOff = new Vector2(1, 1);

    [Header("Timing & Effects")]
    public float attackDelay = 0.1f;
    public float knockbackForce = 5f;
    public float stunDuration = 0.5f;
    public string targetTag = "Player";

    protected override IEnumerator PerformAttack(CombatHandler ch)
    {
        yield return new WaitForSeconds(attackDelay);

        Vector2 inDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        Vector2 size, offset;
        bool diag = false;

        if (inDir.y > 0.5f && Mathf.Abs(inDir.x) < 0.1f)
        {
            size = hbUpSize;
            offset = hbUpOff;
        }
        else if (inDir.y < -0.5f && Mathf.Abs(inDir.x) < 0.1f)
        {
            size = hbDownSize;
            offset = hbDownOff;
        }
        else if (inDir.y > 0.5f && Mathf.Abs(inDir.x) > 0.5f)
        {
            size = hbDiagSize;
            offset = hbDiagOff;
            diag = true;
        }
        else
        {
            size = hbHorizontalSize;
            offset = hbHorizontalOff;
        }

        if (ch.transform.localScale.x < 0)
            offset.x = -offset.x;

        Vector2 kb = offset.normalized * knockbackForce;
        Debug.DrawRay(ch.transform.position, kb, Color.cyan, 0.5f);

        var hbGO = new GameObject("Hitbox");

        hbGO.transform.position = ch.transform.position + (Vector3)offset;
        hbGO.transform.localScale = size;

        if (diag)
        {
            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            hbGO.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        hbGO.transform.SetParent(ch.transform, worldPositionStays: true);

        if (hitboxSprite != null)
        {
            var sr = hbGO.AddComponent<SpriteRenderer>();
            sr.sprite = hitboxSprite;
            sr.color = hitboxColor;
            sr.sortingOrder = 100;
        }

        var bc = hbGO.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;

        var hb = hbGO.AddComponent<Hitbox>();
        hb.Setup(kb, stunDuration, targetTag, 0.1f, ch.SelfCollider, offset);

        Destroy(hbGO, hb.lifetime + 0.05f);
    }
}