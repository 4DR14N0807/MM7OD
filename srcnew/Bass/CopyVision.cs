using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.Graphics.Glsl;

namespace MMXOnline;

public class CopyVision : Weapon {
	public static CopyVision netWeapon = new();

    public CopyVision() : base() {
        index = (int)RockWeaponIds.CopyVision;
        weaponSlotIndex = 2;
        weaponBarBaseIndex = 0;
        //weaponBarIndex = (int)RockWeaponBarIds.CopyVision;
        killFeedIndex = 0;
        maxAmmo = 7;
        ammo = maxAmmo;
		switchCooldown = 0.75f;//gambiarrita
        rateOfFire = 2f;
		
        //shootSounds = new List<string>() {"", "", "", ""};
        description = new string[] {"Create a clone that attack automatically.", "Can only have one clone at once."};
    }

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		if (player.character is Bass bass) {
			new CopyVisionClone(bass.getShootPos(), player, bass.xDir, player.getNextActorNetId(), bass.ownedByLocalPlayer, rpc: true);
		}
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;
        Bass? bass = character as Bass;
		
		new CopyVisionClone(shootPos, player, character.xDir, character.player.getNextActorNetId(), true);
		
	}
	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) {
			return false;
		}
		if (player.character is Bass { cVclone: not null }) {
    		return false;
		}
		return true;}
}
public class CopyVisionLemon : Projectile {

	public CopyVisionLemon(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		CopyVision.netWeapon, pos, xDir, 240, 1, player, "copy_vision_lemon",
		0, 0.075f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.525f;
		fadeSprite = "copy_vision_lemon_fade";
		projId = (int)ProjIds.RaySplasher;
		projId = (int)BassProjIds.BassLemon;
	}}

public class CopyVisionClone : Actor {
	int state = 0;
	float cloneShootTime;
	float cloneTime;
	//define the rateOfFire of the clone.
	float rateOfFire = 0.15f;
	Bass? bass;	
	public CopyVisionClone(Point pos, Player player, int xDir, ushort netId, bool ownedByLocalPlayer, bool rpc = false) :
		base("copy_vision_start", pos, netId, ownedByLocalPlayer, false) {
		bass = player.character as Bass;
		if (bass != null) bass.cVclone = this;
		useGravity = false;
		this.xDir = xDir;
		netOwner = player;
		netActorCreateId = NetActorCreateId.RaySplasherTurret;
		if (rpc) {
			createActorRpc(player.id);
		}
	}



	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		cloneTime += Global.spf;
		if(ownedByLocalPlayer && netOwner.weapon is CopyVision){
			if(netOwner.input.isPressed(Control.Shoot, netOwner)){destroySelf();}
		}
		if(isAnimOver()){
			state = 1;
		}
		if(state == 1){
			changeSprite("copy_vision_clone", true);
			cloneShootTime += Global.spf;
			if (cloneShootTime > rateOfFire) {
				Point? shootPos = getFirstPOI() != null ? getFirstPOI() : pos;
				new CopyVisionLemon(shootPos.Value, xDir, netOwner,netOwner.getNextActorNetId(), rpc: true);
				cloneShootTime = 0;
			}
		} if(cloneTime >= 2f){
			destroySelf();
		}
	}
	public override void onDestroy() {
		base.onDestroy();
		new Anim(
				pos.clone(), "copy_vision_exit",xDir,
				netOwner.getNextActorNetId(), true, sendRpc: true
			);
		if (bass != null) bass.cVclone = null!;
	}
	
}
