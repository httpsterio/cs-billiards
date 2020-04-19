using Jypeli;

// TODO LISÄÄ LYÖNTIÄÄNI JA PALLOJEN TÖRMÄYS

/// <summary>
/// Luokka joka vastaa ääniefektien mäppäyksestä ja tarjoaa funktiot jolla ääniä voi toistaa.
/// </summary>
public class SFX
{
    readonly SoundEffect error = Game.LoadSoundEffect("false");
    readonly SoundEffect seina = Game.LoadSoundEffect("wall");
    readonly SoundEffect intro = Game.LoadSoundEffect("intro");
    readonly SoundEffect bg = Game.LoadSoundEffect("bg");
    readonly SoundEffect ball = Game.LoadSoundEffect("ball");
    readonly SoundEffect fail = Game.LoadSoundEffect("fail");
    readonly SoundEffect win = Game.LoadSoundEffect("win");
    readonly SoundEffect power = Game.LoadSoundEffect("power");

    private double VOLUMELEVEL = 0.2;

    /// <summary>
    /// Get/Set ääniefektien äänenvoimakkuuden.
    /// </summary>
    public double VolumeLevel
    {
        get { return VOLUMELEVEL; }
        set { VOLUMELEVEL = value; }
    }

    public void PlayWall()
    {
        seina.Play(VolumeLevel, 0, 0);
    }
    public void PlayError()
    {
        error.Play(VolumeLevel, 0, 0);
    }

    public void PlayMusic()
    {
        intro.Play(VolumeLevel, 0, 0);
        Timer.SingleShot(6.79, delegate { bg.Play(VolumeLevel, 0, 0); });
        Timer.CreateAndStart(143.8, delegate { bg.Play(VolumeLevel, 0, 0); });
    }

    public void PlayBall()
    {
        ball.Play(VolumeLevel, 0, 0);
    }

    public void PlayFail()
    {
        fail.Play(VolumeLevel, 0, 0);
    }

    public void PlayWin()
    {
        win.Play(VolumeLevel, 0, 0);
    }

    public void PlayPower()
    {
        power.Play(VolumeLevel, 0, 0);
    }

    public void StopPower()
    {
        power.Stop();
    }

    public void StopMusic()
    {
        intro.Stop();
        bg.Stop();
    }

}