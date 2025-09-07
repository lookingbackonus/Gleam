using UnityEngine;

public class PlayerDandelionState : PlayerState
{
    private bool isJumping = false;

    private Camera cam;
    private Vector3 moveVec;
    private float originalPlayerMass;
    private GroundCollisionChecker groundChecker;

    public PlayerDandelionState(Player player, PlayerStateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        InitializeState();

        if (!AttachSeedToPlayer())
        {
            ExitToIdle();
            return;
        }

        SetupGroundCollisionCheck();
        TriggerCollisionCount();
    }

    void InitializeState()
    {
        cam = Camera.main;
        isJumping = false;
        originalPlayerMass = player.rb.mass;
        player.rb.mass = 0.05f;
        player.anim.Play("Spring_One_Hanging");
    }

    bool AttachSeedToPlayer()
    {
        GameObject attachPoint = GameObject.Find("AttachPoint");
        if (attachPoint == null)
        {
            Debug.LogError("AttachPoint not found!");
            return false;
        }

        if (attachPoint.transform.childCount > 0)
        {
            player.dan = null;
            return false;
        }

        player.dan.transform.SetParent(attachPoint.transform);
        player.dan.transform.localPosition = Vector3.zero;
        player.dan.transform.localRotation = Quaternion.identity;

        return true;
    }

    void TriggerCollisionCount()
    {
        DandelionSpawner spawner = GameObject.FindFirstObjectByType<DandelionSpawner>();
        if (spawner != null)
        {
            spawner.OnPlayerSeedCollision();
            Debug.Log($"무지개 카운트 증가: {spawner.GetCollisionCount()}/{spawner.GetCollisionThreshold()}");
        }
    }

    void ExitToIdle()
    {
        player.rb.mass = originalPlayerMass;
        stateMachine.ChangeState(player.idleState);
    }

    public override void Update()
    {
        base.Update();

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        HandleSpaceInput();
        HandleMovement();
    }

    void HandleSpaceInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            PerformJump();
        }
    }

    void HandleMovement()
    {
        if (!isJumping)
        {
            HandleNormalMovement();
        }
        else
        {
            HandleAirControl();
        }
    }

    void PerformJump()
    {
        ReleaseCurrentSeed();
        player.rb.mass = originalPlayerMass;
        isJumping = true;
        player.anim.Play("Spring_AirJump");

        Vector3 jumpDirection = CalculateJumpDirection();
        Vector3 jumpForce = player.playerObj.transform.up.normalized * player.jumpPower +
                           jumpDirection * (player.moveSpeed * 0.4f);

        player.rb.AddForce(jumpForce, ForceMode.Impulse);
    }

    void ReleaseCurrentSeed()
    {
        if (player.dan != null)
        {
            player.dan.transform.SetParent(null);

            dandelionSeed seedScript = player.dan.GetComponent<dandelionSeed>();
            if (seedScript != null)
            {
                seedScript.isBeingRidden = false;
            }

            Object.DestroyImmediate(player.dan);
            player.dan = null;
        }
    }

    Vector3 CalculateJumpDirection()
    {
        Vector3 direction = Vector3.zero;

        switch (player.currentView)
        {
            case CurrentView.Back:
            case CurrentView.Top:
                direction = cam.transform.forward * yInput + cam.transform.right * xInput;
                direction = new Vector3(direction.x, 0, direction.z);

                if (direction == Vector3.zero)
                    direction = player.playerObj.transform.forward;
                else
                    direction = direction.normalized;
                break;

            case CurrentView.Side:
                direction = new Vector3(0, 0, xInput);

                if (direction == Vector3.zero)
                    direction = Vector3.right;
                else
                    direction = direction.normalized;
                break;
        }

        return direction;
    }

    void HandleNormalMovement()
    {
        Vector3 moveDelta = Vector3.zero;

        switch (player.currentView)
        {
            case CurrentView.Back:
            case CurrentView.Top:
                moveVec = cam.transform.forward * yInput + cam.transform.right * xInput;
                moveVec = new Vector3(moveVec.x, 0, moveVec.z).normalized;

                if (moveVec != Vector3.zero)
                {
                    player.playerObj.transform.forward = yInput == -1 ? -moveVec : moveVec;
                    float speedMultiplier = yInput == -1 ? 0.35f : 1f;
                    moveDelta = moveVec * player.moveSpeed * speedMultiplier * Time.deltaTime;
                }
                break;

            case CurrentView.Side:
                moveVec = new Vector3(0, 0, xInput).normalized;
                moveDelta = moveVec * player.moveSpeed * Time.deltaTime;
                break;
        }

        player.rb.MovePosition(player.rb.position + moveDelta);
    }

    void HandleAirControl()
    {
        Vector3 moveDelta = Vector3.zero;

        switch (player.currentView)
        {
            case CurrentView.Back:
            case CurrentView.Top:
                moveVec = cam.transform.forward * yInput + cam.transform.right * xInput;
                moveVec = new Vector3(moveVec.x, 0, moveVec.z).normalized;

                if (moveVec != Vector3.zero)
                {
                    if (yInput < 0)
                    {
                        player.playerObj.transform.forward = -moveVec;
                    }
                    else if (yInput > 0 || xInput != 0)
                    {
                        player.playerObj.transform.forward = moveVec;
                    }
                }

                moveDelta = moveVec * player.moveSpeed * Time.deltaTime;
                break;

            case CurrentView.Side:
                moveVec = new Vector3(0, 0, xInput).normalized;
                moveDelta = moveVec * player.moveSpeed * Time.deltaTime;
                break;
        }

        if (moveDelta != Vector3.zero)
        {
            player.rb.MovePosition(player.rb.position + moveDelta);
        }
    }

    void SetupGroundCollisionCheck()
    {
        GroundCollisionChecker existingChecker = player.playerObj.GetComponent<GroundCollisionChecker>();
        if (existingChecker != null)
        {
            existingChecker.OnGroundHit -= ExitDandelionState;
            Object.Destroy(existingChecker);
        }

        groundChecker = player.playerObj.AddComponent<GroundCollisionChecker>();
        groundChecker.OnGroundHit += ExitDandelionState;
    }

    void ExitDandelionState()
    {
        if (player.dan != null)
        {
            Object.Destroy(player.dan);
            player.dan = null;
        }

        stateMachine.ChangeState(player.idleState);
    }

    public override void Exit()
    {
        base.Exit();

        player.rb.mass = originalPlayerMass;

        if (groundChecker != null)
        {
            groundChecker.OnGroundHit -= ExitDandelionState;
            Object.Destroy(groundChecker);
        }
    }
    private class GroundCollisionChecker : MonoBehaviour
    {
        public System.Action OnGroundHit;

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                OnGroundHit?.Invoke();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                OnGroundHit?.Invoke();
            }
        }
    }
}