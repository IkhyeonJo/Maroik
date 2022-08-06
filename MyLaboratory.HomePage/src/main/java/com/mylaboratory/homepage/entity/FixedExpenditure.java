package com.mylaboratory.homepage.entity;

import javax.persistence.*;
import java.time.Instant;

@Entity
public class FixedExpenditure {
    @Id
    @Column(name = "Id", nullable = false)
    private Integer id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumns({
            @JoinColumn(name = "PaymentMethod", referencedColumnName = "ProductName", nullable = false),
            @JoinColumn(name = "AccountEmail", referencedColumnName = "AccountEmail", nullable = false)
    })
    private Asset asset;

    @Column(name = "MainClass", nullable = false)
    private String mainClass;

    @Column(name = "SubClass", nullable = false)
    private String subClass;

    @Column(name = "Contents", nullable = false)
    private String contents;

    @Column(name = "Amount", nullable = false)
    private Long amount;

    @Column(name = "MyDepositAsset")
    private String myDepositAsset;

    @Column(name = "DepositMonth", nullable = false)
    private Boolean depositMonth = false;

    @Column(name = "DepositDay", nullable = false)
    private Boolean depositDay = false;

    @Column(name = "MaturityDate", nullable = false)
    private Instant maturityDate;

    @Column(name = "Created", nullable = false)
    private Instant created;

    @Column(name = "Updated", nullable = false)
    private Instant updated;

    @Column(name = "Note")
    private String note;

    @Column(name = "Unpunctuality", nullable = false)
    private Boolean unpunctuality = false;

    public Integer getId() {
        return id;
    }

    public void setId(Integer id) {
        this.id = id;
    }

    public Asset getAsset() {
        return asset;
    }

    public void setAsset(Asset asset) {
        this.asset = asset;
    }

    public String getMainClass() {
        return mainClass;
    }

    public void setMainClass(String mainClass) {
        this.mainClass = mainClass;
    }

    public String getSubClass() {
        return subClass;
    }

    public void setSubClass(String subClass) {
        this.subClass = subClass;
    }

    public String getContents() {
        return contents;
    }

    public void setContents(String contents) {
        this.contents = contents;
    }

    public Long getAmount() {
        return amount;
    }

    public void setAmount(Long amount) {
        this.amount = amount;
    }

    public String getMyDepositAsset() {
        return myDepositAsset;
    }

    public void setMyDepositAsset(String myDepositAsset) {
        this.myDepositAsset = myDepositAsset;
    }

    public Boolean getDepositMonth() {
        return depositMonth;
    }

    public void setDepositMonth(Boolean depositMonth) {
        this.depositMonth = depositMonth;
    }

    public Boolean getDepositDay() {
        return depositDay;
    }

    public void setDepositDay(Boolean depositDay) {
        this.depositDay = depositDay;
    }

    public Instant getMaturityDate() {
        return maturityDate;
    }

    public void setMaturityDate(Instant maturityDate) {
        this.maturityDate = maturityDate;
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

    public Boolean getUnpunctuality() {
        return unpunctuality;
    }

    public void setUnpunctuality(Boolean unpunctuality) {
        this.unpunctuality = unpunctuality;
    }

}