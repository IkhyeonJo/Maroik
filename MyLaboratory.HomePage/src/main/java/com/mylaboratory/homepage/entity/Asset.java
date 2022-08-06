package com.mylaboratory.homepage.entity;

import javax.persistence.*;
import java.time.Instant;

@Entity
public class Asset {
    @EmbeddedId
    private AssetId id;

    @MapsId("accountEmail")
    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "AccountEmail", nullable = false)
    private Account accountEmail;

    @Column(name = "Item", nullable = false)
    private String item;

    @Column(name = "Amount", nullable = false)
    private Long amount;

    @Column(name = "MonetaryUnit", nullable = false, length = 45)
    private String monetaryUnit;

    @Column(name = "Created", nullable = false)
    private Instant created;

    @Column(name = "Updated", nullable = false)
    private Instant updated;

    @Column(name = "Note")
    private String note;

    @Column(name = "Deleted", nullable = false)
    private Boolean deleted = false;

    public AssetId getId() {
        return id;
    }

    public void setId(AssetId id) {
        this.id = id;
    }

    public Account getAccountEmail() {
        return accountEmail;
    }

    public void setAccountEmail(Account accountEmail) {
        this.accountEmail = accountEmail;
    }

    public String getItem() {
        return item;
    }

    public void setItem(String item) {
        this.item = item;
    }

    public Long getAmount() {
        return amount;
    }

    public void setAmount(Long amount) {
        this.amount = amount;
    }

    public String getMonetaryUnit() {
        return monetaryUnit;
    }

    public void setMonetaryUnit(String monetaryUnit) {
        this.monetaryUnit = monetaryUnit;
    }

    public Instant getCreated() {
        return created;
    }

    public void setCreated(Instant created) {
        this.created = created;
    }

    public Instant getUpdated() {
        return updated;
    }

    public void setUpdated(Instant updated) {
        this.updated = updated;
    }

    public String getNote() {
        return note;
    }

    public void setNote(String note) {
        this.note = note;
    }

    public Boolean getDeleted() {
        return deleted;
    }

    public void setDeleted(Boolean deleted) {
        this.deleted = deleted;
    }

}