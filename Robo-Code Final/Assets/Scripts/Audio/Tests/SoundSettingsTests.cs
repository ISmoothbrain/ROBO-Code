using NUnit.Framework;

public class SoundSettingsTests
{
    [Test]
    public void SetMusicVolume_StoresTheValue()
    {
        SoundSettings settings = new SoundSettings();

        settings.SetMusicVolume(0.3f);

        Assert.AreEqual(0.3f, settings.MusicVolume);
    }

    [Test]
    public void SetSFXVolume_StoresTheValue()
    {
        SoundSettings settings = new SoundSettings();

        settings.SetSFXVolume(0.7f);

        Assert.AreEqual(0.7f, settings.SFXVolume);
    }

    [Test]
    public void MusicAndSFXVolume_AreIndependent()
    {
        SoundSettings settings = new SoundSettings();

        settings.SetMusicVolume(0.2f);
        settings.SetSFXVolume(0.9f);

        Assert.AreEqual(0.2f, settings.MusicVolume);
        Assert.AreEqual(0.9f, settings.SFXVolume);
    }

    [Test]
    public void SetMusicVolume_AboveMaximum_IsClampedToOne()
    {
        SoundSettings settings = new SoundSettings();

        settings.SetMusicVolume(1.5f);

        Assert.AreEqual(1f, settings.MusicVolume);
    }

    [Test]
    public void SetSFXVolume_BelowMinimum_IsClampedToZero()
    {
        SoundSettings settings = new SoundSettings();

        settings.SetSFXVolume(-0.5f);

        Assert.AreEqual(0f, settings.SFXVolume);
    }
}
